using Objekterkennung_RoboETH._3_Model;
using System;
using System.Collections.Concurrent; // WICHTIG für BlockingCollection
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;              // WICHTIG für CancellationToken
using System.Threading.Tasks;
using static Objekterkennung_RoboETHCore._3_Model.PointCloudProcessing;

namespace Objekterkennung_RoboETHCore._3_Model
{
    /// <summary>
    /// Repräsentiert einen einzelnen Fahr- oder Greifbefehl für den Roboter.
    /// </summary>
    /// <param name="RawData">Der rohe TCP-String (z.B. "MP 100, 100, 70")</param>
    /// <param name="TimeoutMs">Maximale Wartezeit in Millisekunden, bevor ein Fehler geworfen wird</param>
    /// <param name="Description">Lesbare Beschreibung für Logs und Debugging</param>
    public record RobotCommand(string RawData, int TimeoutMs, string Description, whPoint3D TargetPos = null);

    /// <summary>
    /// Hilfsklasse für die Speicherung Soll-Koordinaten und für den späteren Ist-Soll-Vergleich zur Bestimmung, ob
    /// physische Roboterbewegung abgeschlossen ist
    /// </summary>
    public class whPoint3D
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public whPoint3D(float x, float y, float z) { this.X = x; this.Y = y; this.Z = z;}
    }

    /// <summary>
    /// Verwaltet die asynchrone Abarbeitung von Roboterbefehlen (Producer-Consumer-Pattern)
    /// </summary>
    public class Robot_Befehlspipeline : IDisposable
    {
        private BlockingCollection<RobotCommand> _queue = new();
        private CancellationTokenSource _cts = new();
        private Task _workerTask;
        private readonly Action<string> _sendToRobotAction;
        private readonly object _stateLock = new();
        private readonly Roboter roboter_instance;

        // NEU Adrian Sommer, 10.06.2026
        // Flag verhindert Not-Aus-Loop

        private bool _isEmergencyMode = false;

        public Robot_Befehlspipeline(Action<string> sendAction)
        {
            _sendToRobotAction = sendAction;
            StartWorker();
        }

        private void StartWorker()
        {
            // Erstellen von neue Queue, falls  alte geschlossen wurde
            if (_queue.IsCompleted) _queue = new BlockingCollection<RobotCommand>();
            _workerTask = Task.Run(async () => await ProcessQueueAsync(_cts.Token));
        }

        public void Enqueue(string rawData, int timeoutMs, string description, whPoint3D targetPos = null) //=null := optionales arg
        {
            lock (_stateLock)
            {
                if (_isEmergencyMode)
                {
                    Debug.WriteLine($"[PIPELINE ABGELEHNT] System im Not-Aus {description}");
                    return;
                }
                _queue.Add(new RobotCommand(rawData, timeoutMs, description, targetPos));
            }
        }

        private async Task ProcessQueueAsync(CancellationToken token)
        {
            foreach (var command in _queue.GetConsumingEnumerable(token))
            {
                if (token.IsCancellationRequested) break;

                // 1. Befehl senden
                _sendToRobotAction.Invoke(command.RawData);

                // 2. Warten: nutzen von TimeoutMs, um sicherzugehen, dass der Roboter
                // die Bewegung physikalisch beendet hat, BEVOR nach seiner Position gefragt wird
                await Task.Delay(command.TimeoutMs, token);

                // Kleine Pause zwischen den Befehlen, damit sich der Controller erholt
                await Task.Delay(200, token);
            }
        }
        public void EmergencyStopAndClear()
        {
            lock (_stateLock)
            {
                if (_isEmergencyMode) return;
                _isEmergencyMode = true;
                _cts.Cancel(); // Bricht das await Task.Delay sofort ab

                // Roboter-Puffer "totprügeln"
                try
                {
                    _sendToRobotAction.Invoke("RS");
                    Thread.Sleep(50); // Ganz kurze Pause
                }
                catch { }
                while (_queue.TryTake(out _)) { }
                Debug.WriteLine("[PIPELINE] NOT-AUS! Puffer geleert.");
            }
        }

        // NEU Adrian Sommer, 10.06.2026
        // Quittiert den Fehler und startet alles neu
        public void ResetSystem()
        {
            lock (_stateLock)
            {
                _isEmergencyMode = false;
                _cts = new CancellationTokenSource();
                StartWorker();
                Debug.WriteLine("[PIPELINE] System zurückgesetzt.");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _queue.Dispose();
        }
    }
}