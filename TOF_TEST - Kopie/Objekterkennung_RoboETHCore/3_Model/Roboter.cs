using NPOI.OpenXmlFormats.Dml.Diagram;
using Objekterkennung_RoboETHCore._3_Model;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Objekterkennung_RoboETHCore._3_Model.PointCloudProcessing;

namespace Objekterkennung_RoboETH._3_Model
{
    
    public class Roboter
    {
        //-----------------Attribute-----------------//
        #region Attribute
        private bool _isHoming = false; // NEU: Flag für Homing-Zustand
        private const string aAdress = "127.0.0.1";
        private const int aPort = 32572;
        private Socket aSocket;
        private Point[] aMagazin=new Point[5];
        private int aMagazinCounter = 0;

        //-- Pipelineattribute -- NEU Adrian Sommer 18.03.2026
        public Robot_Befehlspipeline Pipeline {  get; private set; }
        private Task _receiverTask;
        private CancellationTokenSource _receiveCts = new CancellationTokenSource();

        private TaskCompletionSource<whPoint3D> _whTaskCompletionSource;
        private readonly object _whLock = new object();
        private whPoint3D _lastKnownPosition = new whPoint3D(553.1f, 202.1f, 300.0f); // Startposition


        //-----------------Parameter-----------------//
        private int aOffsetX = 113;             
        private int aOffsetY = 497;             
        private int aOffsetZ = 70;
        private double aScalingX = 1;     
        private double aScalingY = 1;
        #endregion
        //-----------------Konstruktor-----------------//
        #region Konstruktor
        public Roboter()
        {
            try
            {
                IPHostEntry hostInfo = Dns.GetHostByName(aAdress);     
                IPEndPoint ep = new IPEndPoint(hostInfo.AddressList[0], aPort);
                aSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                aSocket.Connect(ep);
            }
            catch (Exception ex)
            {
                throw new Exception("Fehler beim Herstellen der Verbindung zum Server", ex);
            }
            // --- Pipeline-Initialisierung --- NEU Adrian Sommer 18.03.2026
            this.Pipeline = new Robot_Befehlspipeline(this.SendDataLowLevel);
            this.StartListeningForAcks();


            this.aMagazin[0]= new Point(175, 225);
            this.aMagazin[1] = new Point(220, 250);
            this.aMagazin[2] = new Point(180, 275);
            this.aMagazin[3] = new Point(220, 300);
            this.aMagazin[4] = new Point(180, 330);

            //---- NEU: Adrian Sommer 23.03.2026----//
            this.Pipeline.Enqueue("RS",5000 ,"Initialisieren: Reset");
            this.Pipeline.Enqueue("SP 5, H", 1000, "Initialisieren: Geschwindigkeit setzen");
            _isHoming = true;
            this.Pipeline.Enqueue("OG", 20000, "Initialisieren: Bezugspunkt anfahren");
            GoToStartPos();
        }
        ~Roboter()
        {
            _receiveCts?.Cancel();
            CloseSocket();
        }
        #endregion
        //-----------------Get-Set-Methoden------------//
        #region Get-Set-Methoden
        public bool GetConnectionStatus()
        {
            return this.aSocket.Connected;
        }
        #endregion
        //-----------------Methoden-----------------//
        #region Methoden
        public void ConnectToServer()
        {
            try
            {
                IPHostEntry hostInfo = Dns.GetHostByName(aAdress);      
                IPEndPoint ep = new IPEndPoint(hostInfo.AddressList[0], aPort);
                this.aSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.aSocket.Connect(ep);
                if (!this.aSocket.Connected) 
                {
                    throw new Exception("Verbindung zum Server konnte nicht hergestellt werden");

                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Herstellen der Verbindung zum Server: {ex.Message}" );
            }

        }
        public void SendData(string pData)
        {
            //----NEU: Adrian Sommer 23.03.2026----//
            //Statt jeden "manuellen" Befehl direkt an das TCP-Socket zu senden, wird dieser direkt in die Warteschlange eingereiht.//

            this.Pipeline.Enqueue(pData, 5000, $"Manueller Befehl: {pData}");    
        }
        public string RecieveDataSocket()
        {
            byte[] buffer = new byte[1024];
            int irx = aSocket.Receive(buffer);
            char[] chars = new char[irx];

            Decoder decoder = Encoding.UTF8.GetDecoder();
            int charLen = decoder.GetChars(buffer, 0, irx, chars, 0);
            String recv = new String(chars);
            Thread.Sleep(800);

            return recv;

        }
        public void CloseSocket()
        {
            aSocket.Close();

        }
        public List<PoseAttributes> ChangeKoordinations(List<PoseAttributes> pPoints)
        {
            for (int i = 0; i < pPoints.Count; i++)
            {
                PoseAttributes original = pPoints[i];
                PoseAttributes transformed = new PoseAttributes();

                // Koordinaten tauschen und negieren
                double transformedX = -original.Y;
                double transformedY = -original.X;

                // Skalieren und Verschieben
                transformedX = transformedX * this.aScalingX + this.aOffsetX;
                transformedY = transformedY * this.aScalingY + this.aOffsetY;

                //TESTEN ob die Kameramatrix scheiße ist & Neu-Kalibrierung hilft!

                transformed.X = (int)transformedX;
                transformed.Y = (int)transformedY;
                transformed.Z = original.Z;

                // Yaw anpassen
                float yawCorrection = (float)(Math.Atan2(transformed.X, transformed.Y) * (180.0 / Math.PI));
                transformed.Yaw = original.Yaw + yawCorrection;

                // Neigung anpassen:
                transformed.Neigung = -1 * (90 - original.Neigung);

                transformed.Roll = original.Roll;

                pPoints[i] = transformed;
            }

            return pPoints;
        }
        public void GoToStartPos()
        {
            // Logik wie bei ET2022, nur über pipeline
            this.Pipeline.Enqueue("MP +553.1,+202.1,+300.0,.0,.0", 5000, "Fahre zur Startposition");
            this.Pipeline.Enqueue("GO", 100, "Startposition: Greifer oeffnen");
        }

        public void ResetMagazin()
        {
            this.aMagazinCounter = 0;
        }
        //
        //--- Pipeline-Logik --- NEU Adrian Sommer 18.03.2026
        //
        private void SendDataLowLevel(string pData)
        {
            try
            {
                Debug.WriteLine($"[SOCKET SENDE VERSUCH]: {pData}");
                //
                //
                byte[] data = System.Text.Encoding.ASCII.GetBytes(pData);
                aSocket.Send(data, SocketFlags.None);
                Thread.Sleep(800); // WICHTIG: Kurze, blockierende Pause, damit der Roboter nicht überfordert wird
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Senden des Befehls: {ex.Message}");
            }
        }

        private void StartListeningForAcks()
        {
            _receiverTask = Task.Run(() =>
            {
                byte[] buffer = new byte[1518];
                while (!_receiveCts.Token.IsCancellationRequested && GetConnectionStatus())
                {
                    try
                    {
                        if (aSocket.Available > 0)
                        {
                            int bytesRead = aSocket.Receive(buffer);
                            string rawResponse = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);

                            // --- NEU: Prüfen ob es eine WH-Antwort ist ---
                            // Wenn die Antwort Koordinaten enthält (z.B. keine XML-Tags wie <ERROR> hat, sondern Zahlen)
                            if (!rawResponse.Contains("<ERROR>") && !rawResponse.Contains("OK"))
                            {
                                try
                                {
                                    // Beispiel-Parsing für Movemaster-Koordinaten-String: "230, 450, 70, -90, 0"
                                    var parts = rawResponse.Split(',');
                                    if (parts.Length >= 3)
                                    {
                                        int x = (int)double.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                        int y = (int)double.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                        int z = (int)double.Parse(parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture);

                                        lock (_whLock)
                                        {
                                            _whTaskCompletionSource?.TrySetResult(new whPoint3D(x, y, z));
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[WH] Fehler beim Parsen der Live-Koordinaten: {ex.Message}");
                                }
                            }

                            string errorMessage = StrInStr(rawResponse, "<ERROR>", "</ERROR>");
                            string message = StrInStr(rawResponse, "<MESSAGE>", "</MESSAGE>");

                            if (int.TryParse(errorMessage, out int errorCode))
                            {
                                // 1. Homing-Status Logik
                                if (_isHoming && errorCode == 2)
                                {
                                    System.Diagnostics.Debug.WriteLine("[INFO] Roboter ist im Homing (Busy)... ignoriere Code 2.");
                                    // Wir prüfen, ob das Homing jetzt fertig ist (falls der Roboter "OK" sendet)
                                    if (rawResponse.Contains("MP") || rawResponse.Contains("OK")) _isHoming = false;
                                    continue; // Weiter im Loop, nicht stoppen!
                                }

                                // 2. Kritische Fehler-Erkennung (nur wenn KEIN Homing aktiv)
                                if (errorCode != 1 || message.Contains("Port not Open") || message.Contains("Timeout"))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[!!!] KRITISCHER FEHLER: Code {errorCode} | Nachricht: {message}. Stoppe Pipeline!");
                                    this.Pipeline.EmergencyStopAndClear();
                                }
                                else
                                {
                                    // Alles okay
                                    string time = DateTime.Now.ToString("HH:mm:ss.fff");
                                    System.Diagnostics.Debug.WriteLine($"[{time}] [BRIDGE] Code {errorCode} (Erfolg) empfangen.");

                                    // Falls wir gehomed haben und jetzt ein OK kam, Homing-Flag löschen
                                    if (_isHoming) _isHoming = false;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Socket Lesefehler: {ex.Message}");
                        this.Pipeline.EmergencyStopAndClear();
                        break;
                    }
                }
            }, _receiveCts.Token);
        }
        //Helper-Funktion um Strings des Roboters zu Parsen 
        private string StrInStr(string data, string start, string end) 
        {
            try
            {
                int startIndex = data.IndexOf(start);
                if (startIndex == -1) return "";

                startIndex += start.Length;
                int endIndex = data.IndexOf(end, startIndex);
                if (endIndex == -1) return "";

                return data.Substring(startIndex, endIndex - startIndex);
            }
            catch { return ""; }
        }

        public void SendCoordinatesViaPipeline(List<PoseAttributes> pPoints)
        {
            _lastKnownPosition = new whPoint3D(553.1f, 202.1f, 300.0f);
            foreach (var p in pPoints)
            {
                whPoint3D target = new whPoint3D((float)p.X, (float)p.Y, (float)p.Z);
                whPoint3D transit = new whPoint3D((float)p.X, (float)p.Y, (float)this.aOffsetZ);

                // 1. Anfahrt (Transit-Punkt)
                int dauerAnfahrt = BerechneFahrzeit(_lastKnownPosition.X, _lastKnownPosition.Y, _lastKnownPosition.Z, transit.X, transit.Y, transit.Z);
                Pipeline.Enqueue($"MP {transit.X}, {transit.Y}, {transit.Z}, {(int)p.Neigung}, {(int)p.Yaw}", dauerAnfahrt, "Anfahrt Objekt", transit);
                _lastKnownPosition = transit;

                // 2. Absenken
                int dauerAbsenken = BerechneFahrzeit(_lastKnownPosition.X, _lastKnownPosition.Y, _lastKnownPosition.Z, target.X, target.Y, target.Z);
                Pipeline.Enqueue($"MP {target.X}, {target.Y}, {target.Z}, {(int)p.Neigung}, {(int)p.Yaw}", dauerAbsenken, "Absenken", target);
                _lastKnownPosition = target;

                // 3. Greifen & Zurück
                Pipeline.Enqueue("GC", 2000, "Greifer schließen");

                int dauerAnheben = BerechneFahrzeit(_lastKnownPosition.X, _lastKnownPosition.Y, _lastKnownPosition.Z, transit.X, transit.Y, transit.Z);
                Pipeline.Enqueue($"MP {transit.X}, {transit.Y}, {transit.Z}, -90, 0", dauerAnheben, "Anheben", transit);
                _lastKnownPosition = transit;

                // Magazin-Aufruf
                EnqueueGoToMagazin(ref _lastKnownPosition);
            }
            GoToStartPos();
        }

        //Einreihen der Befehlsabfolge um Objekte in das Magazin ablegen zu können
        //19.05.2026: Adrian Sommer
        private void EnqueueGoToMagazin(ref whPoint3D lastPos)
        {
            if (this.aMagazinCounter == 5)
            {
                throw new Exception("Das Magazin ist voll. Bitte leeren Sie das Magazin und bestätigen es");
            }

            Point p = aMagazin[aMagazinCounter];

            // Zieldefinitionen
            whPoint3D magOver = new whPoint3D((float)p.X, (float)p.Y, (float)this.aOffsetZ);
            whPoint3D magInside = new whPoint3D((float)p.X, (float)p.Y, 6.0f);

            // 1. Fahrt über Magazinplatz
            int d1 = BerechneFahrzeit(lastPos.X, lastPos.Y, lastPos.Z, magOver.X, magOver.Y, magOver.Z);
            Pipeline.Enqueue($"MP {p.X}, {p.Y}, {this.aOffsetZ}, -90, 0", d1, $"Fahrt über Magazinplatz {aMagazinCounter + 1}", magOver);
            lastPos = magOver;

            // 2. Absenken ins Magazin
            int d2 = BerechneFahrzeit(lastPos.X, lastPos.Y, lastPos.Z, magInside.X, magInside.Y, magInside.Z);
            Pipeline.Enqueue($"MP {p.X}, {p.Y}, 6, -90, 0", d2, "Absenken ins Magazin", magInside);
            lastPos = magInside;

            // 3. Objekt loslassen (keine Bewegung, daher feste Zeit)
            Pipeline.Enqueue("GO", 2500, "Objekt loslassen");

            // 4. Aus dem Magazin hochfahren
            int d3 = BerechneFahrzeit(lastPos.X, lastPos.Y, lastPos.Z, magOver.X, magOver.Y, magOver.Z);
            Pipeline.Enqueue($"MP {p.X}, {p.Y}, {this.aOffsetZ}, -90, 0", d3, "Aus dem Magazin hochfahren", magOver);
            lastPos = magOver;

            // 5. Magazin Position sichern (keine Distanz -> 1500ms Minimum)
            //Pipeline.Enqueue($"MP {p.X}, {p.Y}, {this.aOffsetZ}, -90, 0", 1000, "Magazin Position sichern", magOver);

            this.aMagazinCounter++;
        }

        // NEU Adrian Sommer am 10.06.2026
        // Abfragen der aktuellen Position des Roboters (synchron und entkoppelt von der Pipeline)
        // Mit WH (Where) direkt über Socket gesendet, in StartListeningForAcks wird die Position/ der return-value von GetCurrentPositionAsync
        // dekodiert und mit Semaphoren entschieden, wann der nächste befehl in der Pipe abgearbeitet wird -> dynamisierung der befehlsabfolge und
        // verzicht auf statische Thread.Sleep()s oder Task.Delay()s

        // hat sich leider als nicht funktional erwiesen, da per RS232C nur direkt in den Befehlpuffer des roboters geschrieben werden kann
        // positionsabfragen während einer bewegung sind so leider nicht möglich (gerne korrigieren falls falsch, ansatz ist an sich gut)
        // nachfolgend wurde ein ansatz der fahrtzeitenberechnung per inverser kinematik eingesetzt, euklidisch wäre möglich wenn MS statt MP verwendet wird
        public async Task<whPoint3D> GetCurrentPositionAsync(CancellationToken token)
        {
            lock (_whLock)
            {
                _whTaskCompletionSource = new TaskCompletionSource<whPoint3D>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            // Sende den WH Befehl direkt ohne Pipeline-Verzögerung
            byte[] data = System.Text.Encoding.ASCII.GetBytes("WH");
            aSocket.Send(data, SocketFlags.None);

            // Warte auf die Antwort aus dem Hintergrund-Thread oder auf den Timeout/Abbruch
            using (token.Register(() => _whTaskCompletionSource?.TrySetCanceled()))
            {
                try
                {
                    // Maximal 1 Sekunde auf die Antwort des einzelnen WH-Befehls warten
                    var delayTask = Task.Delay(1000, token);
                    var completedTask = await Task.WhenAny(_whTaskCompletionSource.Task, delayTask);

                    if (completedTask == _whTaskCompletionSource.Task)
                    {
                        return await _whTaskCompletionSource.Task;
                    }
                    else
                    {
                        Debug.WriteLine("[WH] Timeout bei Positionsabfrage.");
                        return null; // Timeout beim Lesen
                    }
                }
                catch
                {
                    return null;
                }
            }
        }
        private int BerechneFahrzeitEuklidisch(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            float dist = (float)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) + Math.Pow(z2 - z1, 2));

            // v_eff = 94 mm/s
            float v_eff = 94.0f;

            // Zeit in ms: (Distanz / Geschwindigkeit) * 1000
            // Faktor 1.8 als Sicherheitsaufschlag für Rampen und Gelenk-Interp.
            int timeMs = (int)((dist / v_eff) * 1000 * 1.8f);

            // Minimum 1000ms, damit Greifer/Kurze-Fahrten nicht crashen
            return Math.Max(timeMs, 2000);
        }
        private int BerechneFahrzeit(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            // 1. Gelenkwinkel für Start- und Zielpunkt berechnen
            double[] startWinkel = BerechneGelenkwinkel(x1, y1, z1);
            double[] zielWinkel = BerechneGelenkwinkel(x2, y2, z2);

            // 2. Maximalgeschwindigkeiten der Achsen 
            double[] vmax = { 120.0, 72.0, 109.0, 100.0, 163.0 };
            double sp_faktor = 0.247; // Dein SP 5 Override

            double maxZeit = 0.0;

            // 3. Berechnung der Zeit pro Achse (Führungsachsen-Prinzip)
            for (int i = 0; i < 5; i++)
            {
                double delta = Math.Abs(zielWinkel[i] - startWinkel[i]);
                double v_eff = vmax[i] * sp_faktor;
                double t_achse = delta / v_eff;

                if (t_achse > maxZeit)
                    maxZeit = t_achse;
            }

            // 4. Umrechnung in ms + Sicherheitsaufschlag für Rampen
            // Faktor 1.2 reicht bei echter IK oft aus, da wir die Max-Geschwindigkeit direkt nutzen
            int timeMs = (int)(maxZeit * 1000 * 1.2f);

            return Math.Max(timeMs, 500); 
        }

        // Hilfsfunktion zur IK-Berechnung
        private double[] BerechneGelenkwinkel(float x, float y, float z)
        {
            // Konstanten: Robi-Parameter
            double d1 = 300.0, a2 = 250.0, a3 = 160.0, a4 = 179.0;
            double pitch = 0.0; 

            // Theta 1 
            double t1 = Math.Atan2(x, y) * (180.0 / Math.PI);

            // Kinematische Entkopplung
            double r = Math.Sqrt(x * x + y * y);
            double rw = r - a4 * Math.Cos(pitch * Math.PI / 180.0);
            double zw = z - a4 * Math.Sin(pitch * Math.PI / 180.0);

            // Schulter/Ellbogen (Kosinussatz)
            double D_sq = rw * rw + Math.Pow(zw - d1, 2);
            double cos_t3 = (D_sq - a2 * a2 - a3 * a3) / (2 * a2 * a3);

            // Schutz gegen numerische Fehler außerhalb des Arbeitsraums
            cos_t3 = Math.Max(-1.0, Math.Min(1.0, cos_t3));
            double t3 = Math.Acos(cos_t3) * (180.0 / Math.PI);

            double phi1 = Math.Atan2(zw - d1, rw) * (180.0 / Math.PI);
            double phi2 = Math.Atan2(a3 * Math.Sin(t3 * Math.PI / 180.0), a2 + a3 * Math.Cos(t3 * Math.PI / 180.0)) * (180.0 / Math.PI);
            double t2 = phi1 - phi2;

            double t4 = pitch - t2 - t3;
            double t5 = 0.0; // Roll

            return new double[] { t1, t2, t3, t4, t5 };
        }
        #endregion
    }
}

