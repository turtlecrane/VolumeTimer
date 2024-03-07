using System;
using System.Timers;
using System.Windows;
using NAudio.CoreAudioApi;

namespace VolumeTimer
{
    public partial class MainWindow : Window
    {
        private Timer timer;
        private MMDevice defaultDevice;

        public MainWindow()
        {
            InitializeComponent();

            MMDeviceEnumerator devEnum = new MMDeviceEnumerator();
            defaultDevice = devEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            int minutes;
            if (!int.TryParse(TimerInput.Text, out minutes))
            {
                MessageBox.Show("Invalid input for minutes.");
                return;
            }

            int volume;
            if (!int.TryParse(VolumeInput.Text, out volume))
            {
                MessageBox.Show("Invalid input for volume.");
                return;
            }

            timer = new Timer(1000 * 60 * minutes);
            timer.Elapsed += (s, args) => {
                this.Dispatcher.Invoke(() => {
                    defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume / 100f;
                    defaultDevice.AudioEndpointVolume.Mute = volume == 0;
                });
            };
            timer.AutoReset = false;
            timer.Start();
        }
    }
}