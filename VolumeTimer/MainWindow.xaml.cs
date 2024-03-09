using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NAudio.CoreAudioApi;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VolumeTimer
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private Timer timer;
        private MMDevice defaultDevice;

        private bool isClockTimer = true;
        private bool isMinuteTimer;
        private bool isSecondsTimer;

        private System.Windows.Forms.NotifyIcon notifyIcon = null;
        private System.Windows.Forms.MenuItem timerMenuItem;
        private string setTimerInfo;

        public MainWindow()
        {
            InitializeComponent();
            setTimerInfo = "타이머가 설정되지 않았습니다.";

            MMDeviceEnumerator devEnum = new MMDeviceEnumerator();
            defaultDevice = devEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.DoubleClick += (s, args) => this.Show();  // 사용자가 아이콘을 더블 클릭하면 창을 보여줍니다.
            notifyIcon.Icon = new System.Drawing.Icon("./icon.ico");  // 아이콘 파일 경로
            notifyIcon.Visible = true;

            // 컨텍스트 메뉴를 생성
            System.Windows.Forms.ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();

            // 타이머 정보를 보여주는 메뉴 항목 생성
            timerMenuItem = new System.Windows.Forms.MenuItem();
            timerMenuItem.Text = setTimerInfo;
            contextMenu.MenuItems.Add(timerMenuItem);

            // '종료' 메뉴 항목을 설정하고 컨텍스트 메뉴에 추가
            System.Windows.Forms.MenuItem exitMenuItem = new System.Windows.Forms.MenuItem();
            exitMenuItem.Text = "종료";
            exitMenuItem.Click += new EventHandler(ExitApplication);
            contextMenu.MenuItems.Add(exitMenuItem);

            // 컨텍스트 메뉴를 notifyIcon에 연결
            notifyIcon.ContextMenu = contextMenu;
        }

        /// <summary>
        /// 설정하기 버튼을 눌렀을때의 액션
        /// </summary>
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            int volume = (int)VolumeSlider.Value;

            //시계설정 타이머인경우
            if (isClockTimer)
            {
                StartTimer("시", ClockTimer_HourInput.Text, volume, "분", ClockTimer_MinuteInput.Text);
            }
            //분설정 타이머인 경우
            else if (isMinuteTimer)
            {
                StartTimer("분", MinuteTimer_MinuteInput.Text, volume);
            }
            //초설정 타이머인 경우
            else if (isSecondsTimer)
            {
                StartTimer("초", SecondsTimer_SecondsInput.Text, volume);
            }
        }

        /// <summary>
        /// 타이머 시작하는 함수
        /// </summary>
        /// <param name="unit">해당 타이머의 정보 (시,분,초)</param>
        /// <param name="input">입력값</param>
        /// <param name="volume">볼륨의 양</param>
        private void StartTimer(string unit, string input, int volume, string c_Minutes = null, string c_input = null)
        {
            if (!isClockTimer)
            {
                int time;
                bool isParsed = int.TryParse(input, out time);
                if (!isParsed)
                {
                    MessageBox.Show($"{unit} 입력칸이 비어있습니다.\n\n몇{unit}후 타이머가 실행될지 설정해 주세요.");
                    return;
                }
                if (time <= 0)
                {
                    MessageBox.Show($"0{unit} 이하의 타이머는 설정할 수 없습니다.");
                    return;
                }
                MessageBox.Show($"{time}{unit} 후에 {volume}% 볼륨이 설정됩니다.");
                setTimerInfo = $"{time}{unit} 후 {volume}% 볼륨.";

                //타이머 설정
                SetTimer(time, volume);
            }
            else
            {
                int hour;
                int minute;
                bool isHourParse = int.TryParse(input, out hour);
                bool isMinuteParse = int.TryParse(c_input, out minute);

                MessageBox.Show($"{hour}{unit} {minute}{c_Minutes}에 {volume}% 볼륨이 설정됩니다.");
                setTimerInfo = $"{hour}{unit} {minute}{c_Minutes}에 {volume}% 볼륨.";

                //타이머 설정
                SetTimerForClock(hour, minute, volume);
            }
        }

        /// <summary>
        /// 시계타이머 전용 타이머 설정
        /// </summary>
        /// <param name="_hour">몇시</param>
        /// <param name="_minutes">몇분에</param>
        /// <param name="_volume">이 볼륨으로 설정</param>
        private void SetTimerForClock(int _hour, int _minutes, int _volume)
        {
            // 현재 시간을 가져오기
            DateTime currentTime = DateTime.Now;

            // 설정한 시간을 가져오기
            DateTime setTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, _hour, _minutes, 0);

            // 설정한 시간이 이미 지났다면 경고 출력
            if (setTime < currentTime)
            {
                MessageBox.Show("이미 지난 시간입니다.\n현재 남아있는 시각으로 설정해주세요");
                return;
            }

            // 설정 시간까지의 시간(밀리초 단위)를 계산
            int waitTime = (int)(setTime - currentTime).TotalMilliseconds;

            // 타이머를 생성하고 
            timer = new Timer(waitTime);
            timer.Elapsed += (s, args) => {
                //설정 시간이 되면
                this.Dispatcher.Invoke(() =>
                {
                    //볼륨을 조절하는 함수를 호출
                    SetVolume(_volume);
                });
            };
            timerMenuItem.Text = setTimerInfo;
            timer.AutoReset = false;
            timer.Start();  // 타이머 시작
            StartButton.IsEnabled = false; //설정하기 버튼 비활성화
            DeleteButton.IsEnabled = true; //리셋 버튼 활성화
        }

        /// <summary>
        /// 타이머 설정하는 함수 (분, 초로 설정)
        /// </summary>
        /// <param name="i">몇분 or 몇초</param>
        /// <param name="v">설정된 볼륨의 양</param>
        private void SetTimer(int i, int v)
        {
            int timeInterval;

            switch (true)
            {
                case bool b when isMinuteTimer:
                    timeInterval = 60;
                    break;
                case bool b when isSecondsTimer:
                    timeInterval = 1;
                    break;
                default:
                    timeInterval = 1;
                    break;
            }

            timer = new Timer(1000 * timeInterval * i);
            timer.Elapsed += (s, args) => {
                this.Dispatcher.Invoke(() =>
                {
                    SetVolume(v);
                });
            };

            timerMenuItem.Text = setTimerInfo;
            timer.AutoReset = false;
            timer.Start(); //타이머 시작
            StartButton.IsEnabled = false; //설정하기 버튼 비활성화
            DeleteButton.IsEnabled = true; //리셋 버튼 활성화
        }

        /// <summary>
        /// 타이머가 종료된 후, 시스템 볼륨을 설정한 값으로 설정함
        /// </summary>
        /// <param name="state">볼륨값</param>
        private void SetVolume(object state)
        {
            int _volume = (int)state;
            defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = _volume / 100f;
            defaultDevice.AudioEndpointVolume.Mute = _volume == 0;
            setTimerInfo = "타이머가 종료되었습니다!";
            timerMenuItem.Text = setTimerInfo;
            StartButton.IsEnabled = true; //설정하기 버튼 활성화
            DeleteButton.IsEnabled = false; //리셋 버튼 활성화
        }

        #region UI컴포넌트 액션

        /// <summary>
        /// 시계로 설정하는 라디오버튼의 액션 (기본값)
        /// </summary>
        private void Radio_HourMinute_Checked(object sender, RoutedEventArgs e)
        {
            // 이벤트 핸들러가 로딩 중인지 확인
            if (!IsLoaded)
                return;
            Grid_HourMinute.Visibility = Visibility.Visible;
            Grid_Minute.Visibility = Visibility.Collapsed;
            Grid_Second.Visibility = Visibility.Collapsed;
            isClockTimer = true;
            isMinuteTimer = false;
            isSecondsTimer = false;
        }

        /// <summary>
        /// 분으로 설정하는 라디오 버튼의 액션
        /// </summary>
        private void Radio_Minute_Checked(object sender, RoutedEventArgs e)
        {
            Grid_HourMinute.Visibility = Visibility.Collapsed;
            Grid_Minute.Visibility = Visibility.Visible;
            Grid_Second.Visibility = Visibility.Collapsed;

            isClockTimer = false;
            isMinuteTimer = true;
            isSecondsTimer = false;
        }

        /// <summary>
        /// 초로 설정하는 라디오 버튼의 액션
        /// </summary>
        private void Radio_Second_Checked(object sender, RoutedEventArgs e)
        {
            Grid_HourMinute.Visibility = Visibility.Collapsed;
            Grid_Minute.Visibility = Visibility.Collapsed;
            Grid_Second.Visibility = Visibility.Visible;

            isClockTimer = false;
            isMinuteTimer = false;
            isSecondsTimer = true;
        }


        /// <summary>
        /// 텍스트 박스에 숫자만 입력 가능하도록 - 정규식 함수
        /// </summary>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// 시계설정시, 시간 텍스트박스에 0~23까지의 숫자만 입력 가능하도록 제어
        /// </summary>
        private void Timer_HourInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(ClockTimer_HourInput.Text, out int hour))
            {
                if (hour < 0 || hour > 23)
                {
                    ClockTimer_HourInput.Text = "0";
                }
            }
            else
            {
                ClockTimer_HourInput.Text = "0";
            }
        }

        /// <summary>
        /// 시계설정시, 분 텍스트박스에 0~59까지의 숫자만 입력 가능하도록 제어
        /// </summary>
        private void Timer_MinuteInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(ClockTimer_MinuteInput.Text, out int C_minute))
            {
                if (C_minute < 0 || C_minute > 59)
                {
                    ClockTimer_MinuteInput.Text = "0";
                }
            }
            else
            {
                ClockTimer_MinuteInput.Text = "0";
            }
        }

        /// <summary>
        /// 볼륨 설정 슬라이더의 값을 볼륨라벨의 값에 실시간으로 표시
        /// </summary>
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int volume = (int)Math.Round(e.NewValue);
            VolumeLabel.Content = $"{volume}%";
        }

        /// <summary>
        /// 타이머 재설정 버튼 액션
        /// </summary>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
                StartButton.IsEnabled = true; //설정하기 버튼 비활성화
                DeleteButton.IsEnabled = false; //리셋 버튼 활성화
                setTimerInfo = "타이머가 설정되지 않았습니다.";
                timerMenuItem.Text = setTimerInfo;
                MessageBox.Show("타이머가 삭제되었습니다.");
            }
            else
            {
                MessageBox.Show("삭제할 타이머가 없습니다.");
            }
        }

        #endregion

        /// <summary>
        /// 윈도우 창이 닫혀도 백그라운드에서 계속 진행되게 하는 함수
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;  // 취소 이벤트를 설정하여 창이 닫히는 것을 방지합니다.
            this.Hide();  // 창을 숨깁니다.
        }

        /// <summary>
        /// 프로그램 종료 함수
        /// </summary>
        private void ExitApplication(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
