using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using CreativeTask.Model;

namespace CreativeTask.ViewModel
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ResultItem>? _results;
        public ObservableCollection<ResultItem> Results
        {
            get
            {
                if (_results == null) _results = new ObservableCollection<ResultItem>();
                return _results;
            }
            set
            {
                _results = value;
                OnPropertyChanged(nameof(Results));
            }
        }
        private List<string> _domens = new List<string>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task RunProcessAsync()
        {
            try
            {
                List<Comment> comments = await GetCommentsAsync();
                List<Post> posts = await GetPostsAsync();

                Dictionary<string, Dictionary<string, int>> domainCountByTitle = CountCommentsByDomainAndTitle(comments, posts);

                // Строим таблицу из словаря в объекты
                List<ResultItem> table = new List<ResultItem>();

                // Добавляем заголовки
                foreach (var title in domainCountByTitle.Keys)
                {
                    var titleItem = new ResultItem { Title = title };
                    table.Add(titleItem);

                    // Добавляем комментарии для каждого домена
                    foreach (var domain in domainCountByTitle[title].Keys)
                    {
                        titleItem.CommentsByDomain.Add(domain, domainCountByTitle[title][domain]);
                    }
                }

                Results = new ObservableCollection<ResultItem>(table);

                SaveResultsToExcelFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// GET запрос комментариев с ресурса с проверкой статуса, возвращает список объектов Comment
        /// </summary>
        /// <returns></returns>
        private async Task<List<Comment>> GetCommentsAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync("https://jsonplaceholder.typicode.com/comments");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<List<Comment>>();
            }
        }
        /// <summary>
        /// GET запрос постов с ресурса с проверкой статуса, возвращает список объектов Post
        /// </summary>
        /// <returns></returns>
        private async Task<List<Post>> GetPostsAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync("https://jsonplaceholder.typicode.com/posts");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<List<Post>>();
            }
        }
        /// <summary>
        /// Формирует "двумерный словарь" из Заголовков, доменов и их количества
        /// </summary>
        /// <param name="comments"></param>
        /// <param name="posts"></param>
        /// <returns></returns>
        private Dictionary<string, Dictionary<string, int>> CountCommentsByDomainAndTitle(List<Comment> comments, List<Post> posts)
        {
            Dictionary<string, Dictionary<string, int>> domainCountByTitle = new Dictionary<string, Dictionary<string, int>>();

            foreach (var comment in comments)
            {
                // Находим соответствующий пост для комментария
                var post = posts.FirstOrDefault(p => p.Id == comment.PostId);

                if (post != null && !String.IsNullOrEmpty(comment.Email))
                {
                    // Извлекаем домен из email (последняя часть после точки)
                    string[] emailParts = comment.Email.Split('@');
                    if (emailParts.Length == 2)
                    {
                        string[] domainParts = emailParts[1].Split('.');
                        if (domainParts.Length > 1)
                        {
                            string? domain = domainParts.Last();
                            string? title = post.Title;

                            if (String.IsNullOrEmpty(domain) || String.IsNullOrEmpty(title)) continue;

                            //Если заголовок первый раз всречается, то создаем новую пару
                            if (!domainCountByTitle.ContainsKey(title))
                            {
                                domainCountByTitle[title] = new Dictionary<string, int>();
                            }
                            
                            //если домен первый раз встречается, зажаем единицу, иначе инкримируем
                            if (domainCountByTitle[title].ContainsKey(domain))
                                domainCountByTitle[title][domain]++;
                            else
                                domainCountByTitle[title][domain] = 1;

                            //формирование отдельного списка доменов для удобного и быстрого формирования таблицы
                            if (!_domens.Contains(domain))
                                _domens.Add(domain);
                        }
                    }
                }
            }

            return domainCountByTitle;
        }



        /// <summary>
        /// Сохраняем результирующую таблицу в корневую папку в файл .xlsx
        /// </summary>
        private void SaveResultsToExcelFile()
        {
            try
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Results");

                    // Добавляем заголовки столбцов
                    worksheet.Cells[1, 1].Value = "Домены";
                    //сортируем и добавляем строки с доменами
                    var domenRow = 2;
                    _domens.Sort();
                    foreach (var domen in _domens)
                    {
                        worksheet.Cells[domenRow, 1].Value = $".{domen}";
                        domenRow++;
                    }

                    var columnIndex = 2;
                    foreach (var titleItem in Results)
                    {
                        //добавляем заголовки столбцов
                        worksheet.Cells[1, columnIndex].Value = titleItem.Title;


                        foreach (var domainCount in titleItem.CommentsByDomain)
                        {
                            //ищем в таблице нужный стобец и строку и заполняем его значением
                            var rowIndex = 2;

                            while (!String.IsNullOrEmpty(worksheet.Cells[rowIndex, 1].Text))
                            {
                                if (worksheet.Cells[rowIndex, 1].Text.Contains(domainCount.Key))
                                {
                                    worksheet.Cells[rowIndex, columnIndex].Value = domainCount.Value;
                                    break;
                                }
                                else rowIndex++;
                            }
                        }
                        columnIndex++;
                    }

                    // Сохраняем файл Excel
                    var fileInfo = new FileInfo("Results.xlsx");
                    package.SaveAs(fileInfo);

                    MessageBox.Show("Результаты сохранены в файл Results.xlsx", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

