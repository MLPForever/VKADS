using BaseTemplateForWPF;
using Microsoft.Win32;
using OpenQA.Selenium.Chrome;
using System;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using OpenQA.Selenium.Support.UI;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.VisualBasic.FileIO;
using Google.Apis.Auth.OAuth2;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace VKADS.ViewModels
{
    
    [FirestoreData]
    class User
    {       
        [FirestoreProperty]
        public string UserName { get; set; }
        [FirestoreProperty]
        public string UserPassword { get; set; }
        [FirestoreProperty]
        public string UsageHours { get; set; }       
    }

    class MainWindowViewModel : BaseViewModel
    {
        FirestoreDb db;
        ChromeDriver Driver;
        Thread DriverThread;       
        string FilePath = DateTime.Now.ToString("f").Replace(".", "-").Replace(":", "-") + ".csv";
        StringBuilder CSV = new StringBuilder();
        private static readonly Regex _regex = new Regex("[^0-9.-]+");
        #region RegionDict
        Dictionary<string, string> RegionDict = new Dictionary<string, string>
        {
            { "Санкт-Петербург", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=59.938955&location_title=Санкт-Петербург&long=30.315644&price_max={1}&price_min={2}" },
            { "Новосибирск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=55.028217&latitude=55.028217&location_title=Новосибирск&long=82.923451&longitude=82.923451&price_max={1}&price_min={2}"},
            { "Екатеринбург", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=56.839104&latitude=56.839104&location_title=Екатеринбург&long=60.60825&longitude=60.60825&price_max={1}&price_min={2}"},
            { "Нижний Новгород", "https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=56.326797&latitude=56.326797&location_title=Нижний%20Новгород&long=44.006516&longitude=44.006516&price_max={1}&price_min={2}" },
            { "Казань", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=55.760419&location_title=Казань&long=49.190294&price_max={1}&price_min={2}&section=search" },
            { "Омск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=54.991375&location_title=Омск&long=73.371529&price_max={1}&price_min={2}&section=searchr" },
            { "Ростов-на-Дону", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=47.224914&latitude=47.224914&location_title=Ростов-на-Дону&long=39.702276&longitude=39.702276&price_max={1}&price_min={2}" },
            { "Уфа", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=54.726288&latitude=54.726288&location_title=Уфа&long=55.947727&longitude=55.947727&price_max={1}&price_min={2}" },
            { "Пермь", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=58.014965&latitude=58.014965&location_title=Пермь&long=56.246723&longitude=56.246723&price_max={1}&price_min={2}" },
            { "Волгоград", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=48.710678&latitude=48.710678&location_title=Волгоград&long=44.516804&longitude=44.516804&price_max={1}&price_min={2}" },
            { "Воронеж", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=51.65488&latitude=51.65488&location_title=Воронеж&long=39.212949&longitude=39.212949&price_max={1}&price_min={2}" },
            { "Саратов", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=51.530018&latitude=51.530018&location_title=Саратов&long=46.034683&longitude=46.034683&price_max={1}&price_min={2}" },
            { "Краснодар", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=45.035433&latitude=45.035433&location_title=Краснодар&long=38.975712&longitude=38.975712&price_max={1}&price_min={2}" },
            { "Тольятти", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=53.507852&latitude=53.507852&location_title=Тольятти&long=49.420411&longitude=49.420411&price_max={1}&price_min={2}" },
            { "Тюмень", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=57.153534&latitude=57.153534&location_title=Тюмень&long=65.542274&longitude=65.542274&price_max={1}&price_min={2}" },
            { "Ижевск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=56.852676&latitude=56.852676&location_title=Ижевск&long=53.2069&longitude=53.2069&price_max={1}&price_min={2}"},
            { "Барнаул", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=53.346785&latitude=53.346785&location_title=Барнаул&long=83.776856&longitude=83.776856&price_max={1}&price_min={2}"},
            { "Ульяновск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=54.314192&latitude=54.314192&location_title=Ульяновск&long=48.403132&longitude=48.403132&price_max={1}&price_min={2}" },
            { "Оренбург", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=51.768205&latitude=51.768205&location_title=Оренбург&long=55.096964&longitude=55.096964&price_max={1}&price_min={2}"},
            { "Томск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=56.488712&latitude=56.488712&location_title=Томск&long=84.952324&longitude=84.952324&price_max={1}&price_min={2}"},
            { "Кемерово", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=55.355198&latitude=55.355198&location_title=Кемерово&long=86.086847&longitude=86.086847&price_max={1}&price_min={2}"},
            { "Астрахань", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=46.347614&latitude=46.347614&location_title=Астрахань&long=48.030178&longitude=48.030178&price_max={1}&price_min={2}" },
            { "Рязань", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=54.629565&latitude=54.629565&location_title=Рязань&long=39.741917&longitude=39.741917&price_max={1}&price_min={2}"},
            { "Набережные Челны", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=55.741272&latitude=55.741272&location_title=Набережные%20Челны&long=52.403662&longitude=52.403662&price_max={1}&price_min={2}" },
            { "Пенза", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=53.195042&latitude=53.195042&location_title=Пенза&long=45.018316&longitude=45.018316&price_max={1}&price_min={2}"},
            { "Тула", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=54.193122&latitude=54.193122&location_title=Тула&long=37.617348&longitude=37.617348&price_max={1}&price_min={2}" },
            { "Киров", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=58.603595&latitude=58.603595&location_title=Киров&long=49.668023&longitude=49.668023&price_max={1}&price_min={2}"},
            { "Чебоксары", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=56.139918&latitude=56.139918&location_title=Чебоксары&long=47.247728&longitude=47.247728&price_max={1}&price_min={2}"},
            { "Курск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=51.730846&latitude=51.730846&location_title=Курск&long=36.193015&longitude=36.193015&price_max={1}&price_min={2}" },
            { "Улан-Удэ", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=51.834809&latitude=51.834809&location_title=Улан-Удэ&long=107.584547&longitude=107.584547&price_max={1}&price_min={2}"},
            { "Ставрополь", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=45.043317&latitude=45.043317&location_title=Ставрополь&long=41.96911&longitude=41.96911&price_max={1}&price_min={2}" },
            { "Тверь", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=56.859625&latitude=56.859625&location_title=Тверь&long=35.91186&longitude=35.91186&price_max={1}&price_min={2}"},
            { "Иваново", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=57.000353&latitude=57.000353&location_title=Иваново&long=40.97393&longitude=40.97393&price_max={1}&price_min={2}" },
            { "Брянск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=53.243562&latitude=53.243562&location_title=Брянск&long=34.363425&longitude=34.363425&price_max={1}&price_min={2}"},
            { "Сочи", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=43.585482&latitude=43.585482&location_title=Сочи&long=39.723109&longitude=39.723109&price_max={1}&price_min={2}" },
            { "Белгород", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=50.595414&latitude=50.595414&location_title=Белгород&long=36.587277&longitude=36.587277&price_max={1}&price_min={2}"},
            { "Сургут", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=61.254032&latitude=61.254032&location_title=Сургут&long=73.3964&longitude=73.3964&price_max={1}&price_min={2}" },
            { "Владимир", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=56.129057&location_title=Владимир&long=40.406635&price_max={1}&price_min={2}&section=search" },
            { "Архангельск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=64.539911&latitude=64.539911&location_title=Архангельск&long=40.515762&longitude=40.515762&price_max={1}&price_min={2}" },
            { "Нижний Тагил", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=57.907562&latitude=57.907562&location_title=Свердловская%20область%2C%20Нижний%20Тагил&long=59.971474&longitude=59.971474&price_max={1}&price_min={2}" },
            { "Симферополь", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=44.948237&latitude=44.948237&location_title=Республика%20Крым%2C%20Симферополь&long=34.100327&longitude=34.100327&price_max={1}&price_min={2}" },
            { "Волжский", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=48.786127&latitude=48.786127&location_title=Волгоградская%20область%2C%20Волжский&long=44.751229&longitude=44.751229&price_max={1}&price_min={2}" },
            { "Смоленск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=54.782635&latitude=54.782635&location_title=Смоленск&long=32.045287&longitude=32.045287&price_max={1}&price_min={2}" },
            { "Саранск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=54.187433&latitude=54.187433&location_title=Республика%20Мордовия%2C%20Саранск&long=45.183938&longitude=45.183938&price_max={1}&price_min={2}" },
            { "Череповец", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=59.122612&latitude=59.122612&location_title=Вологодская%20область%2C%20Череповец&long=37.90347&longitude=37.90347&price_max={1}&price_min={2}" },
            { "Курган", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=55.441004&latitude=55.441004&location_title=Курган&long=65.341118&longitude=65.341118&price_max={1}&price_min={2}" },
            { "Подольск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=55.431136&latitude=55.431136&location_title=Московская%20область%2C%20Подольск&long=37.544997&longitude=37.544997&price_max={1}&price_min={2}" },
            { "Вологда", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=59.220501&latitude=59.220501&location_title=Вологда&long=39.891523&longitude=39.891523&price_max={1}&price_min={2}" },
            { "Орёл", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=52.970756&latitude=52.970756&location_title=Орёл&long=36.064358&longitude=36.064358&price_max={1}&price_min={2}" },
            { "Петрозаводск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=61.785021&latitude=61.785021&location_title=Республика%20Карелия%2C%20Петрозаводск&long=34.346878&longitude=34.346878&price_max={1}&price_min={2}" },
            { "Нижневартовск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=60.938545&latitude=60.938545&location_title=Ханты-Мансийский%20автономный%20округ%2C%20Нижневартовск&long=76.558902&longitude=76.558902&price_max={1}&price_min={2}" },
            { "Кострома", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=57.767966&latitude=57.767966&location_title=Кострома&long=40.926858&longitude=40.926858&price_max={1}&price_min={2}" },
            { "Йошкар-Ола", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=56.6316&latitude=56.6316&location_title=Республика%20Марий%20Эл%2C%20Йошкар-Ола&long=47.886178&longitude=47.886178&price_max={1}&price_min={2}" },
            { "Новороссийск", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=44.723771&latitude=44.723771&location_title=Краснодарский%20край%2C%20Новороссийск&long=37.768813&longitude=37.768813&price_max={1}&price_min={2}" },
            { "Стерлитамак", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=53.630403&latitude=53.630403&location_title=Республика%20Башкортостан%2C%20Стерлитамак&long=55.930825&longitude=55.930825&price_max={1}&price_min={2}" },
            { "Таганрог", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=47.208735&latitude=47.208735&location_title=Ростовская%20область%2C%20Таганрог&long=38.936694&longitude=38.936694&price_max={1}&price_min={2}" },
            { "Сыктывкар", @"https://vk.com/classifieds?catalog_context=classifieds&classifieds_category_id={0}&distance_max=50000&lat=61.668797&latitude=61.668797&location_title=Республика%20Коми%2C%20Сыктывкар&long=50.836497&longitude=50.836497&price_max={1}&price_min={2}" }
        };
        #endregion
        public List<string> Regions { get; } = new();
        public List<string> Days { get; } = new List<string> { "Вчера", "Сегодня" };
        public List<int> Hours { get; } = new();
        List<string> Povtor = new();

        public MainWindowViewModel()
        {

            string path = AppDomain.CurrentDomain.BaseDirectory + "vkads-f5d15-firebase-adminsdk-jm17b-458ac9941b.json";
            try
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);
                db = FirestoreDb.Create("vkads-f5d15");

            }
            catch (Exception e)
            {
                string messageBoxText = e.Message;
                string caption = "Упс...";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Information;
                MessageBoxResult result;
                result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.OK);
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            FirstTableCommand = new LambdaCommand(OnFirstTableCommandExecuted, CanFirstTableCommandExecute);
            SecondTableCommand = new LambdaCommand(OnSecondTableCommandExecuted, CanSecondTableCommandExecute);
            CreateTable3Command = new LambdaCommand(OnCreateTable3CommandExecuted, CanCreateTable3CommandExecute);
            FormTableCommand = new LambdaCommand(OnFormTableCommandExecuted, CanFormTableCommandExecute);
            ParserCommand = new LambdaCommand(OnParserCommandExecuted, CanParserCommandExecute);
            DriverPathCommand = new LambdaCommand(OnDriverPathCommandExecuted, CanDriverPathCommandExecute);
            StartDriverCommand = new LambdaCommand(OnStartDriverCommandExecuted, CanStartDriverCommandExecute);
            CloseCommand = new LambdaCommand(OnCloseCommandExecuted, CanCloseCommandExecute);
            PathForCSVCommand = new LambdaCommand(OnPathForCSVCommandExecuted, CanPathForCSVCommandExecute);
            AddToFireStore = new LambdaCommand(OnAddToFireStoreExecuted, CanAddToFireStoreExecute);
            GetFirestore = new LambdaCommand(OnGetFirestoreExecuted, CanGetFirestoreExecute);
            ChangeFireStoreCommand = new LambdaCommand(OnChangeFireStoreCommandExecuted, CanChangeFireStoreCommandEecute);
            DeleteFireStoreCommand = new LambdaCommand(OnDeleteFireStoreCommandExecuted, CanDeleteFireStoreCommandExecute);

            foreach (var item in RegionDict.Keys) Regions.Add(item);
            for (int i = 0; i < 24; i++) Hours.Add(i);
            SelectedOtHour = Hours[0];
            SelectedRegion = Regions[0];
            SelectedOTDay = Days[0];
            SelectedDoDay = Days[1];
            SelectedDoHour = Hours[^1];
            if (Properties.Settings.Default.CSVPATH != "None") FilePath = Properties.Settings.Default.CSVPATH + "\\" + DateTime.Now.ToString("f").Replace(".", "-").Replace(":", "-") + ".csv";
        }


        #region IsChangeFireStore 

        private bool _IsChangeFireStore = false;
        public bool IsChangeFireStore
        {
            get => _IsChangeFireStore;
            set => Set(ref _IsChangeFireStore, value);
        }

        #endregion

        #region ContentBTN 

        private string _ContentBTN = "Новый доступ";
        public string ContentBTN
        {
            get => _ContentBTN;
            set => Set(ref _ContentBTN, value);
        }

        #endregion

        #region Table1 

        private List<string> _Table1;
        public List<string> Table1
        {
            get => _Table1;
            set => Set(ref _Table1, value);
        }

        #endregion

        #region Selected 

        private User _Selected;
        public User Selected
        {
            get => _Selected;
            set => Set(ref _Selected, value);
        }

        #endregion

        #region Table2 

        private List<string> _Table2;
        public List<string> Table2
        {
            get => _Table2;
            set => Set(ref _Table2, value);
        }

        #endregion

        #region Table3

        private List<string> _Table3;
        public List<string> Table3
        {
            get => _Table3;
            set => Set(ref _Table3, value);
        }

        #endregion

        #region PathForCSV 

        private string _PathForCSV;
        public string PathForCSV
        {
            get => _PathForCSV;
            set => Set(ref _PathForCSV, value);
        }

        #endregion

        #region OnlyOneRegion 

        private bool _OnlyOneRegion = false;
        public bool OnlyOneRegion
        {
            get => _OnlyOneRegion;
            set => Set(ref _OnlyOneRegion, value);
        }

        #endregion

        #region Category 

        private string _Category;
        public string Category
        {
            get => _Category;
            set => Set(ref _Category, value);
        }

        #endregion

        #region SelectedDoHour 

        private int _SelectedDoHour;
        public int SelectedDoHour
        {
            get => _SelectedDoHour;
            set => Set(ref _SelectedDoHour, value);
        }

        #endregion

        #region SelectedDoDay 

        private string _SelectedDoDay;
        public string SelectedDoDay
        {
            get => _SelectedDoDay;
            set => Set(ref _SelectedDoDay, value);
        }

        #endregion

        #region SelectedOtHour 

        private int _SelectedOtHour;
        public int SelectedOtHour
        {
            get => _SelectedOtHour;
            set => Set(ref _SelectedOtHour, value);
        }

        #endregion

        #region SelectedOTDay 

        private string _SelectedOTDay;
        public string SelectedOTDay
        {
            get => _SelectedOTDay;
            set => Set(ref _SelectedOTDay, value);
        }

        #endregion

        #region MaxPrice 

        private int _MaxPrice;
        public int MaxPrice
        {
            get => _MaxPrice;
            set => Set(ref _MaxPrice, value);
        }

        #endregion

        #region MinPrice 

        private int _MinPrice = 0;
        public int MinPrice
        {
            get => _MinPrice;
            set => Set(ref _MinPrice, value);
        }

        #endregion

        #region SelectedRegion 

        private string _SelectedRegion;
        public string SelectedRegion
        {
            get => _SelectedRegion;
            set => Set(ref _SelectedRegion, value);
        }

        #endregion

        #region Title 

        private string _Title = "VKADS";
        public string Title
        {
            get => _Title;
            set => Set(ref _Title, value);
        }

        #endregion

        #region Test 

        private string _Test;
        public string Test
        {
            get => _Test;
            set => Set(ref _Test, value);
        }

        #endregion

        #region UserPassword 

        private string _UserPassword;
        public string UserPassword
        {
            get => _UserPassword;
            set => Set(ref _UserPassword, value);
        }

        #endregion

        #region UserName 

        private string _UserName;
        public string UserName
        {
            get => _UserName;
            set => Set(ref _UserName, value);
        }

        #endregion

        #region UsageHour 

        private string _UsageHour = "";
        public string UsageHour
        {
            get => _UsageHour;
            set => Set(ref _UsageHour, value);
        }

        #endregion


        #region FireList 

        private ObservableCollection<User> _FireList = new();
        public ObservableCollection<User> FireList
        {
            get => _FireList;
            set => Set(ref _FireList, value);
        }

        #endregion

        #region Methods      
       

        #region readfile
        private List<string> readFile(string fileNames)
        {


            TextFieldParser parser = new TextFieldParser(fileNames);
            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            List<string> t = new();
            while (!parser.EndOfData)
            {
                //Processing row
                string[] fields = parser.ReadFields();
                foreach (string field in fields)
                {
                    t.Add(field);
                }
            }
            return t;
        } 
        #endregion

        #region StartDriver
        private void StartDriver()
        {
            ChromeDriverService chromeservice = ChromeDriverService.CreateDefaultService(string.Join("\\", Properties.Settings.Default.DriverPath.Split("\\")[..^1]));
            chromeservice.HideCommandPromptWindow = true;
            Driver = new ChromeDriver(chromeservice);
            Driver.Navigate().GoToUrl(@"https://vk.com/");                
        }
        #endregion

        #region Parse
        private bool Parse(string link)
        {
            try
            {
                Driver.Navigate().GoToUrl(link);
                CSV = new StringBuilder();
                try
                {
                    Thread.Sleep(2000);
                    Driver.FindElement(By.XPath("//*[@id=\"box_layer\"]/div[2]/div/div[1]/div[1]/svg"));
                    return true;
                }
                catch
                {
                    //try
                    //{
                    //    WebDriverWait wdw = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
                    //    wdw.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(ElementNotVisibleException));
                    //    wdw.Until(drv => drv.FindElement(By.XPath("//*[@id=\"react_rootEcommClassifiedsItemCard\"]/div/div[2]/div[1]/div[2]/div/span/span/button")));
                    //    //*[@id="react_rootEcommClassifiedsItemCard"]/div/div[2]/div[1]/div[2]/div/span/span/ button
                    //    wdw.Until(drv => drv.FindElement(By.ClassName("ItemStats")));
                    //}
                    //catch
                    //{
                    //    return true;
                    //}

                    var d = Driver.FindElement(By.ClassName("ItemStats")).Text;
                    var ob = Driver.FindElement(By.XPath("//*[@id=\"react_rootEcommClassifiedsItemCard\"]/div/div[2]/div[1]/div[2]/div/span/span/button")).Text;
                    if (Povtor.Contains(ob)) return true;
                    Povtor.Add(ob);
                    if (!ob.ToLower().Contains("объявлени")) return true;
                    ob = ob.Replace("(", string.Empty);
                    ob = ob.Replace(")", string.Empty);
                    int x = 0;
                    foreach (var item in ob.Split(" "))
                    {
                        try
                        {
                            x = Convert.ToInt32(item);
                            break;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    if (x > 15) return true;
                    try
                    {
                        Thread.Sleep(2500);
                        var ot = Driver.FindElement(By.XPath("//*[@id=\"react_rootEcommClassifiedsItemCard\"]/div/div[2]/div[1]/div[2]/span/div/button")).Text;
                        ot = ot.Replace("\"", string.Empty);
                        x = 0;
                        foreach (var item in ot.Split(" "))
                        {
                            try
                            {
                                x = Convert.ToInt32(item);
                                break;
                            }
                            catch
                            {
                                continue;
                            }
                        }
                        if (x > 3) return true;
                    }
                    catch
                    {                        
                    }

                    var pre = SelectedOTDay.ToLower();
                    var past = SelectedDoDay.ToLower();

                    if (pre != past)
                    {
                        if ((d.ToLower().Contains(pre) & (Convert.ToInt32(d.ToLower().Split(" ")[3].Split(":")[0]) >= SelectedOtHour)) | (d.ToLower().Contains(past) & (Convert.ToInt32(d.ToLower().Split(" ")[3].Split(":")[0]) <= SelectedDoHour)))
                        {
                            Thread.Sleep(500);
                            CSV.AppendLine(link);
                            File.AppendAllText(FilePath, CSV.ToString());
                            return true;
                        }
                        else return true;
                    }
                    else if (pre == past)
                    {
                        if ((d.ToLower().Contains(pre) | (d.ToLower().Contains(past))) & (SelectedOtHour <= Convert.ToInt32(d.ToLower().Split(" ")[3].Split(":")[0])) & (Convert.ToInt32(d.ToLower().Split(" ")[3].Split(":")[0]) <= SelectedDoHour))
                        {
                            Thread.Sleep(500);
                            CSV.AppendLine(link);
                            File.AppendAllText(FilePath, CSV.ToString());
                            return true;
                        }
                        else return true;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                return true;
            }
        }
        #endregion

        #region Preparse
        private bool Preparse(string url)
        {
            Driver.Navigate().GoToUrl(url + "&ref_source=marketplace&section=search&traffic_source=link");
            try
            {
                Thread.Sleep(2000);
                Driver.FindElement(By.XPath("//*[@id=\"box_layer\"]/div[2]/div/div[1]/div[1]/svg"));
                return true;
            }
            catch 
            {
                for (int i = 0; i < 4; i++)
                {
                    Driver.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
                    Thread.Sleep(1000);
                }
                var a = Driver.FindElements(By.XPath("//*[@id=\"react_rootEcommClassifiedsCatalog\"]/div/div[4]/div/div[1]/div[1]/div[2]/div[*]"));
                List<string> links = new();
                foreach (var link in a) try { links.Add(link.FindElement(By.TagName("a")).GetAttribute("href")); } catch { continue; }
                foreach (var link in links)
                {
                    var temp = link.Replace("&screen=vko", string.Empty);
                    temp = temp.Replace("productproduct", "product");
                    bool t = Parse(temp);
                    while (t == false) t = Parse(temp);
                    Thread.Sleep(1000);
                }

                return true;
            }
        }
        #endregion

        #region ParseDriver
        private void ParseDriver()
        {
            if (MaxPrice == 0) MaxPrice = int.MaxValue;

            bool b = false;
            foreach (string key in RegionDict.Keys)
            {
                if (key == SelectedRegion) b = true;
                else if(b==false) continue;
                if (b)
                {
                    bool t = Preparse(string.Format(RegionDict[key], Category, Convert.ToString(MaxPrice) + "00", Convert.ToString(MinPrice) + "00"));
                    while (t == false) t = Preparse(string.Format(RegionDict[key], Category, Convert.ToString(MaxPrice) + "00", Convert.ToString(MinPrice) + "00"));
                }
                if (OnlyOneRegion) break;
            }
        }
        #endregion

        #endregion

        #region Commands

        #region DeleteFireStoreCommand
        public ICommand DeleteFireStoreCommand { get; }
        private bool CanDeleteFireStoreCommandExecute(object p) => true;
        private async void OnDeleteFireStoreCommandExecuted(object p)
        {
            DocumentReference cityRef = db.Collection("users").Document(Selected.UserName).Collection(Selected.UserPassword).Document("Hours");
            await cityRef.DeleteAsync();
            object n = "";
            OnGetFirestoreExecuted(n);
        }
        #endregion

        #region ChangeFireStoreCommand
        public ICommand ChangeFireStoreCommand { get; }
        private bool CanChangeFireStoreCommandEecute(object p) => true;
        public void OnChangeFireStoreCommandExecuted(object p)
        {
            IsChangeFireStore = true;
            User user = Selected;
            UserName = user.UserName;
            UserPassword = user.UserPassword;
            UsageHour = Convert.ToString(user.UsageHours);
            ContentBTN = "Изменить доступ";            
        }
        #endregion

        #region GetFirestore
        public ICommand GetFirestore { get; }
        private bool CanGetFirestoreExecute(object p) => db != null;
        private async void OnGetFirestoreExecuted(object p)
        {
            FireList.Clear();
            CollectionReference usersRef = db.Collection("users");
            var ListDocuments = usersRef.ListDocumentsAsync().ToEnumerable();
            foreach (var Document in ListDocuments)
            {
                var ListCollections = Document.ListCollectionsAsync().ToEnumerable();
                foreach (var Collections in ListCollections)
                {
                    var q = await Collections.GetSnapshotAsync();
                    foreach (var w in q.Documents)
                    {
                        Dictionary<string, object> documents = w.ToDictionary();
                        FireList.Add(new User
                        {
                            UsageHours = (string)documents["time"],
                            UserName = Document.Id,
                            UserPassword = Collections.Id
                        });
                    }
                }
            }
        }
        #endregion

        #region AddToFireStore
        public ICommand AddToFireStore { get; }
        private bool CanAddToFireStoreExecute(object p) => !string.IsNullOrEmpty(UserName) & !string.IsNullOrEmpty(UserPassword) & !string.IsNullOrEmpty(UsageHour) & db != null & !_regex.IsMatch(UsageHour);
        private async void OnAddToFireStoreExecuted(object p)
        {
            DocumentReference usersRef = db.Collection("users").Document(UserName).Collection(UserPassword).Document("Hours");
            var rand = new Random();
            Dictionary<string, object> user = new Dictionary<string, object>
            {
                {"time", Convert.ToString(UsageHour)}
            };
            await usersRef.SetAsync(user);
            UserName = string.Empty;
            UserPassword = string.Empty;
            UsageHour = string.Empty;
            if (IsChangeFireStore)
            {
                IsChangeFireStore = false;
                ContentBTN = "Новый доступ";
            }
            object n = "";
            OnGetFirestoreExecuted(n);
        }
        #endregion

        #region PathForCSVCommand
        public ICommand PathForCSVCommand { get; }
        private bool CanPathForCSVCommandExecute(object p) => true;
        private void OnPathForCSVCommandExecuted(object p)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                PathForCSV = dlg.FileName;
            }
            FilePath = PathForCSV + "\\" + DateTime.Now.ToString("f").Replace(".", "-").Replace(":", "-") + ".csv";
            Properties.Settings.Default.CSVPATH = PathForCSV;
            Properties.Settings.Default.Save();
        }
        #endregion

        #region ParserCommand
        public ICommand ParserCommand { get; }
        private bool CanParserCommandExecute(object p) => Driver != null & !string.IsNullOrEmpty(Category);
        private void OnParserCommandExecuted(object p)
        {
            DriverThread = new Thread(ParseDriver);
            DriverThread.Start();
        }
        #endregion

        #region StartDriverCommand
        public ICommand StartDriverCommand { get; }
        private bool CanStartDriverCommandExecute(object p) => Properties.Settings.Default.DriverPath != "None" & Properties.Settings.Default.CSVPATH != "None";
        private void OnStartDriverCommandExecuted(object p)
        {
            DriverThread = new Thread(StartDriver);
            DriverThread.Start();
        //File.AppendAllText("C:\\Users\\User\\Desktop\\4 июля 2022 г- 21-03.csv", "SSS\n");
        }
        #endregion

        #region DriverPathCommand
        public ICommand DriverPathCommand { get; }
        private bool CanDriverPathCommandExecute(object p) => true;
        private void OnDriverPathCommandExecuted(object p)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true) 
            {
                string temp = openFileDialog.FileName;
                if (temp.Split("/")[^1].Contains("driver.exe"))
                {
                    Properties.Settings.Default.DriverPath = openFileDialog.FileName; 
                    Properties.Settings.Default.Save(); 
                }
                else
                {
                    string messageBoxText = "Выбранный файл не сильно похож на драйвер(";
                    string caption = "Упс...";
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Warning;
                    MessageBoxResult result;

                    result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.OK);
                }
                
            }
        
        }
        #endregion

        #region CloseCommand
        public ICommand CloseCommand { get; }
        private bool CanCloseCommandExecute(object p) => true;
        private void OnCloseCommandExecuted(object p)
        {   
            if (Driver != null) Driver.Quit();
            DriverThread = new(StartDriver);
            Application.Current.Shutdown();
        }
        #endregion

        #region CreateTable3Command
        public ICommand CreateTable3Command { get; }
        private bool CanCreateTable3CommandExecute(object p) => Table1 != null & Table1 != null;
        private void OnCreateTable3CommandExecuted(object p)
        {
            var t1 = Table1;
            var t2 = Table2;
            List<string> t = new();
            string s = "";
            bool flag;
            foreach (var item1 in t1)
            {
                flag = true;
                foreach (var item2 in t2)
                {
                    if (item1 == item2)
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    t.Add(item1);
                }

            }
            Table3 = t;

        }
        #endregion

        #region FormTableCommand
        public ICommand FormTableCommand { get; }
        private bool CanFormTableCommandExecute(object p) => Table3 != null & Properties.Settings.Default.CSVPATH != "None";
        private void OnFormTableCommandExecuted(object p)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ссылка");
            foreach (var item in Table3)
            {
                csv.AppendLine(item);
            }
            File.WriteAllText(Properties.Settings.Default.CSVPATH + "\\" + DateTime.Now.ToString("f").Replace(".", "-").Replace(":", "-") +  "DroppedDublicate.csv", csv.ToString());
        }
        #endregion

        #region FirstTableCommand

        public ICommand FirstTableCommand { get; }
        private bool CanFirstTableCommandExecute(object p) => true;
        private void OnFirstTableCommandExecuted(object p)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                if (openFileDialog.ShowDialog() != true)
                    return;

                Table1 = null;
                Table1 = readFile(openFileDialog.FileName);

            }
            catch
            {
                string messageBoxText = "Скорее всего таблица уже открыта где-то в другом месте. Закройте и попробуйте еще раз";
                string caption = "Ошибка загрузки населения";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Information;
                MessageBox.Show(messageBoxText, caption, button, icon);
            }
        }

        #endregion

        #region SecondTableCommand

        public ICommand SecondTableCommand { get; }
        private bool CanSecondTableCommandExecute(object p) => true;
        private void OnSecondTableCommandExecuted(object p)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                if (openFileDialog.ShowDialog() != true)
                    return;

                Table2 = null;
                Table2 = readFile(openFileDialog.FileName);

            }
            catch
            {
                string messageBoxText = "Скорее всего таблица уже открыта где-то в другом месте. Закройте и попробуйте еще раз";
                string caption = "Ошибка загрузки населения";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Information;
                MessageBox.Show(messageBoxText, caption, button, icon);
            }
        }

        #endregion
        #endregion
    }
}
