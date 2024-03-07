using System;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int volume = (int)Math.Round(e.NewValue);
            VolumeLabel.Content = $"{volume}%";
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            int minutes;
            if (!int.TryParse(Timer_HourInput.Text, out minutes))
            {
                MessageBox.Show("Invalid input for minutes.");
                return;
            }

            int volume = (int)VolumeSlider.Value;

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