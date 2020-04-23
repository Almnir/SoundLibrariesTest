using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SoundLibrariesTest
{
    public partial class Form1 : Form
    {
        string sDestinationFile;

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
            string rndm = new Random().Next().ToString() + "file.";
            //Get the default music directory of Windows:
            sDestinationFile = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) + "\\" + rndm + "wav";

            StringBuilder err = new StringBuilder();
            mciSendString("stop mywav", err, 0, IntPtr.Zero);
            mciSendString("save mywav " + sDestinationFile, err, 0, IntPtr.Zero);
            mciSendString("close mywav", err, 0, IntPtr.Zero);
            if (!string.IsNullOrEmpty(err.ToString()))
            {
                MessageBox.Show(err.ToString());
            }

        }

        private void StopByWINNM()
        {
            mciSendString("stop thisIsMyTag", null, 0, IntPtr.Zero);
            mciSendString("close thisIsMyTag", null, 0, IntPtr.Zero);
        }

        private void buttonStartRecord_Click(object sender, EventArgs e)
        {
            RecordByWINNM();
        }

        private void buttonStopRecord_Click(object sender, EventArgs e)
        {
            StopRecordByWINNM();
        }

        private void buttonStartPlay_Click(object sender, EventArgs e)
        {
            PlayByWINNM();
        }

        private void buttonStopPlay_Click(object sender, EventArgs e)
        {
            StopByWINNM();
        }
    }
}
