using System.Windows;

namespace ETN
{
    public partial class PasswordWindow : Window
    {
        public string Password { get; private set; }
        public bool IsDecrypting { get; private set; }

        public PasswordWindow(string message, bool isDecrypting = false)
        {
            InitializeComponent();
            this.MessageText.Text = message;
            this.IsDecrypting = isDecrypting;

            if (IsDecrypting)
            {
                this.ConfirmPasswordBox.Visibility = Visibility.Collapsed;
                this.MessageText.Text = "请输入密码以解密该文件：";
            }
            else
            {
                this.ConfirmPasswordBox.Visibility = Visibility.Visible;
                this.MessageText.Text = "请输入密码并确认：";
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsDecrypting && PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("密码和确认密码不匹配，请重新输入。", "密码错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.Password = PasswordBox.Password;
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
