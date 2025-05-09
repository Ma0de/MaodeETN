using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;

namespace ETN
{
    public partial class MainWindow : Window
    {
        private string currentFilePath = null;
        private string currentFilePassword = null;
        private bool isTextChanged = false;
        private bool isNewFile = true;

        public MainWindow()
        {
            InitializeComponent();
            this.PreviewKeyDown += new KeyEventHandler(HandleKeyPress);
            NoteTextBox.TextChanged += NoteTextBox_TextChanged;
        }

        private void NoteTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!isNewFile && !this.Title.EndsWith("*"))
            {
                this.Title += "*";
                isTextChanged = true;
            }
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                SaveFile();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void SaveFile()
        {
            if (string.IsNullOrEmpty(currentFilePath) || string.IsNullOrEmpty(currentFilePassword))
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "mdETN加密笔记本文件 (*.mdetn)|*.mdetn",
                    DefaultExt = "mdetn",
                    FileName = "Note.mdetn",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    currentFilePath = saveFileDialog.FileName;
                    currentFilePassword = PromptForPassword("输入加密密码以加密该文件：", false);
                    if (currentFilePassword == null) return;
                }
                else return;
            }

            byte[] encryptedData = EncryptString(NoteTextBox.Text, currentFilePassword);
            File.WriteAllBytes(currentFilePath, encryptedData);

            string fileName = System.IO.Path.GetFileName(currentFilePath);
            this.Title = $"(md)ETN - Encrypted Text Notebook 猫德加密笔记本 ({fileName})";
            isTextChanged = false;
            isNewFile = false;
        }

        private static string PromptForPassword(string message, bool isDecrypting)
        {
            PasswordWindow passwordWindow = new PasswordWindow(message, isDecrypting);
            if (passwordWindow.ShowDialog() == true)
            {
                return passwordWindow.Password;
            }
            return null;
        }

        private byte[] EncryptString(string plainText, string password)
        {
            byte[] salt = GenerateSalt();
            using (var key = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                using (var aesAlg = Aes.Create())
                {
                    aesAlg.Key = key.GetBytes(32);
                    aesAlg.IV = key.GetBytes(16);

                    var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                    using (var msEncrypt = new MemoryStream())
                    {
                        msEncrypt.Write(salt, 0, salt.Length);
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (var swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(plainText);
                            }
                        }
                        return msEncrypt.ToArray();
                    }
                }
            }
        }

        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        private void OpenEncryptedFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "mdETN加密笔记本文件 (*.mdetn)|*.mdetn",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                OpenEncryptedFile(openFileDialog.FileName);
            }
        }

        private void OpenEncryptedFile(string filePath)
        {
            string password = PromptForPassword("输入密码以解密此加密文件：", true);
            if (password == null) return;

            try
            {
                byte[] encryptedData = File.ReadAllBytes(filePath);
                string decryptedText = DecryptString(encryptedData, password);
                NoteTextBox.Text = decryptedText;

                currentFilePath = filePath;
                currentFilePassword = password;
                isNewFile = false;
                isTextChanged = false;

                NoteTextBox.IsUndoEnabled = false;
                NoteTextBox.IsUndoEnabled = true;

                string fileName = System.IO.Path.GetFileName(currentFilePath);
                this.Title = $"(md)ETN - Encrypted Text Notebook 猫德加密笔记本 ({fileName})";
                NoteTextBox.Focus();
                NoteTextBox.CaretIndex = NoteTextBox.Text.Length;
            }
            catch (CryptographicException)
            {
                MessageBox.Show("密码错误，无法解密文件内容。", "解密失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"读取文件时出错: {ioEx.Message}", "文件错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生未知错误: {ex.Message}", "未知错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string DecryptString(byte[] cipherText, string password)
        {
            using (var msDecrypt = new MemoryStream(cipherText))
            {
                byte[] salt = new byte[16];
                msDecrypt.Read(salt, 0, salt.Length);

                using (var key = new Rfc2898DeriveBytes(password, salt, 10000))
                {
                    using (var aesAlg = Aes.Create())
                    {
                        aesAlg.Key = key.GetBytes(32);
                        aesAlg.IV = key.GetBytes(16);

                        var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string filePath = files[0];

                if (System.IO.Path.GetExtension(filePath).ToLower() == ".mdetn")
                {
                    OpenEncryptedFile(filePath);
                }
            }
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void bzxq_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("此项目由猫德一人开发维护，联系猫德请通过github", "详情", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void WindowButton_Click(object sender, RoutedEventArgs e)
        {
            PWindow.Visibility = Visibility.Collapsed;
        }
    }
}
