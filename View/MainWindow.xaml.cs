using System;
using System.Windows;
using CreativeTask.ViewModel;

namespace CreativeTask.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        private async void OnRunButtonClick(object sender, RoutedEventArgs e)
        {
            await _viewModel.RunProcessAsync();
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnInfoButtonClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Нажмите Запустить, программа запросит данные с сервера и сохранит обработанный файл в корневую папку в формате .xlsx");
        }
    }
}

