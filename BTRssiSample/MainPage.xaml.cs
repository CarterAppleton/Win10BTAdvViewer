using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BTRssiSample
{
    /// <summary>
    /// Display all BTLE Beacons that we see
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Advertisement Watcher
        private BluetoothLEAdvertisementWatcher watcher;

        // Whether or not we show devices lacking a friendly name
        private bool showNamelessDevices = false;

        // Remember advertisements by their Bluetooth Address so we don't show duplicate devices
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

                // Use .Active so we always request a scan response and get the friendly name
                this.watcher.ScanningMode = BluetoothLEScanningMode.Active;

                // If we haven't seen an advertisement for 3 seconds, consider the device out of range
                watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(3000);

                // If we see an advertisement from a device with an RSSI less than -120, consider it out of range
                watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -120;

                // Register our callbacks
                watcher.Received += OnAdvertisementReceived;
                watcher.Stopped += OnAdvertisementWatcherStopped;
            }

            watcher.Start();

            // Start updating the advertisement list UI every 0.5 seconds
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += new EventHandler<object>(UpdateAdvertisementList);
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

            // Create a new AdvertisingDevice or update an old one if we've already seen this device
            var deviceData = new AdvertisingDevice();
            if (this.seenAdvertisements.ContainsKey(addr))
            {
                deviceData = this.seenAdvertisements[addr];
            }
            deviceData.LocalName = localName.Length > 0 ? localName : deviceData.LocalName;
            deviceData.RSSI = rssi;
            deviceData.TimeStamp = timestamp;
            deviceData.BluetoothAddress = addr;
            this.seenAdvertisements[addr] = deviceData;
        }

        private void namelessDevicesToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch tSwitch = sender as ToggleSwitch;
            if (tSwitch != null)
            {
                this.showNamelessDevices = tSwitch.IsOn;
            }
        }

        private static bool AdvTimeout(AdvertisingDevice eventArgs)
        {
            return DateTimeOffset.Now.ToUnixTimeSeconds() - eventArgs.TimeStamp.ToUnixTimeSeconds() > 300;
        }

        private void UpdateAdvertisementList(object sender, object e)
        {
            // Grab all the devices that we have seen
            var vals = this.seenAdvertisements.Values.ToList();

            // Filter the advertisements
            vals.RemoveAll(delegate (AdvertisingDevice advertisingDevice)
            {
                // If we only want to show named devices, remove all those without names
                if (!this.showNamelessDevices && (advertisingDevice.LocalName == null || advertisingDevice.LocalName.Length == 0)) return true;

                // Remove devices that we haven't seen a signal from for a while
                return DateTimeOffset.Now.ToUnixTimeSeconds() - advertisingDevice.TimeStamp.ToUnixTimeSeconds() > 300;
            });

            // Sort the devices by RSSI- how "close" they are
            vals.Sort(delegate (AdvertisingDevice arg1, AdvertisingDevice arg2)
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
            public string DisplayName
            {
                get
                {
                    return LocalName != null ? LocalName : "(Name Unknown)";
                }
            }
            public short RSSI { get; set; }
            public DateTimeOffset TimeStamp { get; set; }
            public ulong BluetoothAddress { get; set; }
        }
    }
}