# Win10BTAdvViewer
This is a basic app to view all Bluetooth advertisements around your Windows 10 device.

## Bluetooth LE Advertisements App Development
### Getting started
`Package.appxmanifest > Capabilities > Turn on Bluetooth`

To enable Bluetooth LE for any Windows 10 app, open your Package.appxmanifest, go to Capabilities, and check Bluetooth. This will allow your app to access the Bluetooth APIs. Without this, you can't access Bluetooth!

### Starting a Watcher
In order to see all of the Bluetooth Advertisements around you, you must create a `BluetoothLEAdvertisementWatcher`. This is relatively straightforward:

`var watcher = new BluetoothLEAdvertisementWatcher();`<br>
`watcher.Received += OnAdvertisementReceived;`<br>
`watcher.Stopped += OnAdvertisementWatcherStopped;` <br>
`watcher.Start();`<br>

### Filtering
#### By packet data
There are many ways to filter an advertisement by the data it contains. The `watcher.AdvertisementFilter` property provides many choices. Here, we show how to filter advertisements by manufacturer id (in this case, Microsoft!)

`var manufacturerData = new BluetoothLEManufacturerData()`<br>
`manufacturerData.CompanyId = 0x0006`<br>

If you wanted to be even more specific, say you were making both Bluetooth watches and refridgerators and you only wanted to listen for watches, you could make your manufacturer filter more specific.

`var writer = new DataWriter();`<br>
`writer.WriteUInt16(0x1234); // Some product identifier`<br> 
`manufactuererData.Data = writer.DetachBuffer();`

#### In range
Sometimes you only want to receive advertisements when a device is within a certain distance. With the following, the watcher will only fire when advertisements with RSSI >= -70dBm are seen.

`watcher.SignalStrengthFilter.InRangeThresholdInDBm = -70;`
#### Out of range
Advertisements are often used for sensing proximity or presence. The watcher can also notify you if an advertising device has gone out of range. Just specify an out of range timeout and an out of range threshold. In the following example, we tell the watcher to:

`watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(3000);`<br>
`watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -120;`

### Scanning Modes
The watcher will, by default, scan passively. This means that it will listen to all of the advertisements that are incoming, but never request more information from them. If you want more information, such as a the scan response which often contains the `LocalName` of the device:

`watcher.ScanningMode = BluetoothLEScanningMode.Active;`
