using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BTRssiSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private BluetoothLEAdvertisementWatcher watcher;
        private bool showNamelessDevices = false;

        private Dictionary<ulong, AdvertisingDevice> seenAdvertisements = new Dictionary<ulong, AdvertisingDevice>();

        public MainPage()
        {
            this.InitializeComponent();
            this.StartWatcher();
        }

        public void StartWatcher()
        {
            if (this.watcher == null)
            {
                this.watcher = new BluetoothLEAdvertisementWatcher();
                this.watcher.ScanningMode = BluetoothLEScanningMode.Active;
                watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(3000);
                watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -120;
                watcher.Received += OnAdvertisementReceived;

                watcher.Stopped += OnAdvertisementWatcherStopped;

                System.Diagnostics.Debug.WriteLine("Advertisement watcher started");

            }

            watcher.Start();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += new EventHandler<object>(UpdateAdvertisementList2);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            timer.Start();
        }

        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {
            // We can obtain various information about the advertisement we just received by accessing 
            // the properties of the EventArgs class

            // The timestamp of the event
            DateTimeOffset timestamp = eventArgs.Timestamp;

            // The type of advertisement
            BluetoothLEAdvertisementType advertisementType = eventArgs.AdvertisementType;

            // The received signal strength indicator (RSSI)
            short rssi = eventArgs.RawSignalStrengthInDBm;

            // The BT address of the advertisement
            var addr = eventArgs.BluetoothAddress;

            // The local name of the advertising device contained within the payload, if any
            string localName = eventArgs.Advertisement.LocalName;

            var deviceData = new AdvertisingDevice();
            if(this.seenAdvertisements.ContainsKey(addr))
            {
                deviceData = this.seenAdvertisements[addr];
            }
            deviceData.LocalName = localName.Length > 0 ? localName : deviceData.LocalName;
            deviceData.RSSI = rssi;
            deviceData.TimeStamp = timestamp;
            deviceData.BluetoothAddress = addr;
            this.seenAdvertisements[addr] = deviceData;

            this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //this.UpdateAdvertisementList();
            });
        }

        private void namelessDevicesToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch tSwitch = sender as ToggleSwitch;
            if(tSwitch != null)
            {
                this.showNamelessDevices = tSwitch.IsOn;
            }
        }

        private static bool AdvTimeout(AdvertisingDevice eventArgs)
        {
            return DateTimeOffset.Now.ToUnixTimeSeconds() - eventArgs.TimeStamp.ToUnixTimeSeconds() > 300;
        }

        private void UpdateAdvertisementList2(object sender, object e)
        {
            UpdateAdvertisementList();
        }

        private void UpdateAdvertisementList()
        {
            // Grab all the devices that we have seen
            var vals = this.seenAdvertisements.Values.ToList();

            // Filter the advertisements
            vals.RemoveAll(delegate(AdvertisingDevice advertisingDevice)
            {
                // If we only want to show named devices, remove all those without names
                if (!this.showNamelessDevices && (advertisingDevice.LocalName == null || advertisingDevice.LocalName.Length == 0)) return true;

                // Remove devices that we haven't seen a signal from for a while
                return DateTimeOffset.Now.ToUnixTimeSeconds() - advertisingDevice.TimeStamp.ToUnixTimeSeconds() > 300;
            });

            // Sort the devices by RSSI- how "close" they are
            vals.Sort(delegate(AdvertisingDevice arg1, AdvertisingDevice arg2)
            {
                return arg2.RSSI.CompareTo(arg1.RSSI);
            });

            // Update the xaml list
            advertisingDevicesBox.Items.Clear();
            foreach (AdvertisingDevice val in vals)
            {
                advertisingDevicesBox.Items.Add(val);
            }
        }

        /// <summary>
        /// Invoked as an event handler when the watcher is stopped or aborted.
        /// </summary>
        /// <param name="watcher">Instance of watcher that triggered the event.</param>
        /// <param name="eventArgs">Event data containing information about why the watcher stopped or aborted.</param>
        private async void OnAdvertisementWatcherStopped(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementWatcherStoppedEventArgs eventArgs)
        {
            System.Diagnostics.Debug.WriteLine("Advertisement watcher stopped");

            // Notify the user that the watcher was stopped
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {

            });
        }
        public class AdvertisingDevice
        {
            public string LocalName { get; set; }
            public string DisplayName { get{
                    return LocalName != null ? LocalName : "(Name Unknown)";
                }
                      }

            public short RSSI { get; set; }
            public DateTimeOffset TimeStamp { get; set; }
            public ulong BluetoothAddress { get; set; }


        }
    }


}
