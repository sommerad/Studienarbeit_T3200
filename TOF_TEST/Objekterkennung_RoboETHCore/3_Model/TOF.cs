using GigeVision.Core.Services;     //GigE Schnitstelle
using System;

//using GenICam;                       //GenICam Schnitstelle
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;


namespace Objekterkennung_RoboETHCore._3_Model
{
    //Anleitung :https://github.com/Touseefelahi/GigeVision#get-all-cameras-in-the-network
    // https://github.com/Touseefelahi/GigeVision/blob/master/GigeVisionLibrary.Test.Wpf/MainWindow.xaml.cs
    public class TOF
    {
        private Camera aCamera;
        private bool isLoaded;

       
        public  TOF() 
        {
           
        }
        ~TOF()
        {
            this.aCamera.FrameReady -= FrameReady;
        }

        public async Task InitializeCamera()
        {
            try
            {
                this.aCamera = new Camera();
                GigeVision.Core.NetworkService.AllowAppThroughFirewall();
                var listOfDevices = await this.aCamera.Gvcp.GetAllGigeDevicesInNetworkAsnyc().ConfigureAwait(true);

                if (listOfDevices.Count > 0)
                {
                    this.aCamera.IP = listOfDevices.FirstOrDefault()?.IP;
                    this.aCamera.RxIP = listOfDevices.FirstOrDefault()?.NetworkIP;
                }

                this.aCamera.Payload = 0;   //GGF andere
                this.aCamera.IsMulticast = false;
                this.aCamera.FrameReady += FrameReady;

            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler bei der Kamera-Initialisierung: {ex.Message}");
            }
        }

        public async Task StartCamera()
        {
           
            if (!this.aCamera.Gvcp.IsXmlFileLoaded)
            {
                isLoaded = await this.aCamera.Gvcp.ReadXmlFileAsync();
            }

            if (this.aCamera.IsStreaming)
            {
                await this.aCamera.StopStream().ConfigureAwait(false);
                
            }
            else
            {
               bool sucess= await this.aCamera.StartStreamAsync(GetLocalIPAddress()).ConfigureAwait(false);
                this.aCamera.Height = 480;
                if (!sucess)
                {
                    throw new Exception("Kamera konnte nicht gestartet werden");
                }

            }
        }
       
        //Wird nicht aufgerufen. Bei Height sind falsche Werte drin... WIESO?
        private void FrameReady(object sender, byte[] framedata)
        {
           throw new Exception("Fuktioniert! Frame wurde empfangen");
        }
      /*  public async Task<byte[]> CaptureSingleFrame()
        {
            try
            {
                // Setze die Kamera in den Einzelbildmodus
                await (await aCamera.Gvcp.GetRegister("AcquisitionMode"))
                    .Item1.SetValueAsync("SingleFrame")
                    .ConfigureAwait(false);

                // Aktiviere den Software-Trigger
                await (await aCamera.Gvcp.GetRegister("TriggerMode"))
                    .Item1.SetValueAsync("On")
                    .ConfigureAwait(false);
                await (await aCamera.Gvcp.GetRegister("TriggerSource"))
                    .Item1.SetValueAsync("Software")
                    .ConfigureAwait(false);

                // Event für Bildempfang abonnieren
                TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();
                this.aCamera.FrameReady += (sender, frameData) =>
                {
                    tcs.SetResult(frameData);
                };

                // Software-Trigger auslösen
                await (await aCamera.Gvcp.GetRegister("TriggerSoftware"))
                    .Item1.SetValueAsync(1)
                    .ConfigureAwait(false);

                // Auf das empfangene Bild warten
                byte[] frame = await tcs.Task.ConfigureAwait(false);

                Debug.WriteLine($"Einzelbild empfangen: {frame.Length} Bytes");
                return frame;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Einzelbildaufnahme: {ex.Message}");
                throw;
            }
        }
      */
        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Keine IPv4 Adapter vorhanden");
        }

    }
}
