using CSCore;
using CSCore.Codecs;
using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.SoundOut;
using CSCore.Streams;
using CSCore.Win32;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SoundLibrariesTest
{
    public partial class Form1 : Form
    {
        string sDestinationFile;
        private IWriteable _writer;
        private ISoundOut _soundOut;
        private WasapiCapture _soundIn;
        private IWaveSource _finalSource;
        private readonly ObservableCollection<MMDevice> _devices = new ObservableCollection<MMDevice>();

        public Form1()
        {
            InitializeComponent();
            sDestinationFile = "";
        }

        //MediaPlayer mediaPlayer = new MediaPlayer();

        //private void PlayByMediaPlayer()
        //{
        //    Uri myUri = new Uri(@"e:\1\file_example_OOG_1MG.ogg", UriKind.Absolute);
        //    mediaPlayer.Open(myUri);
        //    mediaPlayer.Play();
        //}

        //private void StopByMediaPlayer()
        //{
        //    mediaPlayer?.Stop();
        //}

        [DllImport("winmm.dll")]
        private static extern long mciSendString(
            string command,
            StringBuilder returnValue,
            int returnLength,
            IntPtr winHandle);

        private void PlayByWINNM()
        {
            if (string.IsNullOrEmpty(sDestinationFile)) return;

            StringBuilder err = new StringBuilder();
            mciSendString("open " + sDestinationFile + " type waveaudio alias thisIsMyTag", err, 0, IntPtr.Zero);
            //mciSendString("open " + sDestinationFile + " type mpegvideo alias thisIsMyTag", err, 0, IntPtr.Zero);
            mciSendString("play thisIsMyTag", err, 0, IntPtr.Zero);
            if (!string.IsNullOrEmpty(err.ToString()))
            {
                MessageBox.Show(err.ToString());
            }
        }

        private void RecordByWINNM()
        {
            StringBuilder err = new StringBuilder();
            mciSendString("open new type waveaudio alias mywav", err, 0, IntPtr.Zero);
            mciSendString("set mywav bitspersample 8 samplespersec 8000 channels 1", err, 0, IntPtr.Zero);
            mciSendString("record mywav", err, 0, IntPtr.Zero);
            if (!string.IsNullOrEmpty(err.ToString()))
            {
                MessageBox.Show(err.ToString());
            }

        }
        private void StopRecordByWINNM()
        {
            GetFilePath();

            StringBuilder err = new StringBuilder();
            mciSendString("stop mywav", err, 0, IntPtr.Zero);
            mciSendString("save mywav " + sDestinationFile, err, 0, IntPtr.Zero);
            mciSendString("close mywav", err, 0, IntPtr.Zero);
            if (!string.IsNullOrEmpty(err.ToString()))
            {
                MessageBox.Show(err.ToString());
            }

        }

        private void GetFilePath()
        {
            string rndm = new Random().Next().ToString() + "file.";
            //Get the default music directory of Windows:
            sDestinationFile = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) + "\\" + rndm + "wav";
        }

        private void StopByWINNM()
        {
            mciSendString("stop thisIsMyTag", null, 0, IntPtr.Zero);
            mciSendString("close thisIsMyTag", null, 0, IntPtr.Zero);
        }

        void StartRecordByCSCore()
        {
            GetFilePath();
            _soundIn = new WasapiCapture();
            _soundIn.Device = (MMDevice)listView1.SelectedItems[0].Tag;
            _soundIn.Initialize();
            var soundInSource = new SoundInSource(_soundIn);
            var singleBlockNotificationStream = new SingleBlockNotificationStream(soundInSource.ToSampleSource());
            _finalSource = singleBlockNotificationStream.ToWaveSource();
            _writer = new WaveWriter(sDestinationFile, _finalSource.WaveFormat);
            byte[] buffer = new byte[_finalSource.WaveFormat.BytesPerSecond / 2];
            soundInSource.DataAvailable += (s, e) =>
            {
                int read;
                while ((read = _finalSource.Read(buffer, 0, buffer.Length)) > 0)
                    _writer.Write(buffer, 0, read);
            };

            _soundIn.Start();

        }

        void StopRecordByCSCore()
        {
            if (_soundIn != null)
            {
                _soundIn.Stop();
                _soundIn.Dispose();
                _soundIn = null;
                _finalSource.Dispose();

                if (_writer is IDisposable)
                    ((IDisposable)_writer).Dispose();
            }
        }

        void StartPlayByCSCore()
        {
            IWaveSource _waveSource = CodecFactory.Instance.GetCodec(sDestinationFile)
                .ToSampleSource()
                .ToMono()
                .ToWaveSource();
            _soundOut = new WasapiOut() { Latency = 100, Device = (MMDevice)comboBox1.SelectedItem };
            _soundOut.Initialize(_waveSource);
            _soundOut.Play();
        }

        void StopPlayByCSCore()
        {
            if (_soundOut != null)
            {
                _soundOut.Stop();
                _soundOut.Dispose();
                _soundOut = null;
            }
                
        }

        private void buttonStartRecord_Click(object sender, EventArgs e)
        {
            StartRecordByCSCore();
        }

        private void buttonStopRecord_Click(object sender, EventArgs e)
        {
            StopRecordByCSCore();
        }

        private void buttonStartPlay_Click(object sender, EventArgs e)
        {
            StartPlayByCSCore();
        }

        private void buttonStopPlay_Click(object sender, EventArgs e)
        {
            StopPlayByCSCore();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            using (var mmdeviceEnumerator = new MMDeviceEnumerator())
            {
                using (
                    var mmdeviceCollection = mmdeviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active))
                {
                    foreach (var device in mmdeviceCollection)
                    {
                        _devices.Add(device);
                    }
                }
            }
            RefreshDevices();
            comboBox1.DataSource = _devices;
            comboBox1.DisplayMember = "FriendlyName";
            comboBox1.ValueMember = "DeviceID";
        }

        private void RefreshDevices()
        {
            listView1.Items.Clear();

            using (var deviceEnumerator = new MMDeviceEnumerator())
            using (var deviceCollection = deviceEnumerator.EnumAudioEndpoints(
                DataFlow.Capture, DeviceState.Active))
            {
                foreach (var device in deviceCollection)
                {
                    var deviceFormat = WaveFormatFromBlob(device.PropertyStore[
                        new PropertyKey(new Guid(0xf19f064d, 0x82c, 0x4e27, 0xbc, 0x73, 0x68, 0x82, 0xa1, 0xbb, 0x8e, 0x4c), 0)].BlobValue);

                    var item = new ListViewItem(device.FriendlyName) { Tag = device };
                    item.SubItems.Add(deviceFormat.Channels.ToString(CultureInfo.InvariantCulture));

                    listView1.Items.Add(item);
                }
            }
        }
        private static WaveFormat WaveFormatFromBlob(Blob blob)
        {
            if (blob.Length == 40)
                return (WaveFormat)Marshal.PtrToStructure(blob.Data, typeof(WaveFormatExtensible));
            return (WaveFormat)Marshal.PtrToStructure(blob.Data, typeof(WaveFormat));
        }

    }
}
