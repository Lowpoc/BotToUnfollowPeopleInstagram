using CsvHelper;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoInsta
{
    public class Instagram : IPageObject , IDisposable
    {
        private readonly IConfigurationRoot _config;
        private IList<Person> _blacklist;
        private IList<string> _whiteList;
        private IWebDriver _webdriver;

        #region HTML ELEMENTS
        private const string AuthFacebook = "//span[@class='KPnG0']";
        private const string SigInFacebookId = "loginbutton";
        private const string EmailInputId = "email";
        private const string PasswordInputId = "pass";
        private const string Avatar = "//a[@class='gmFkV']";
        private const string BtnCloseModalNotification = "//div[@class='mt3GC']";
        private const string FollowingModal = "//div[@class='PZuss']";
        private const string FollowingModalbtn = "//a[@class='-nal3 ']";
        private const string UlOptions = "//ul[@class='k9GMp ']";
        private const string DivModalSeguindo = "//div[@class='eiUFA']";
        private const string FollowersModal = "//div[@class='isgrP']";
        private const string UnFollowBtn = "aOOlW";
        #endregion

        public Instagram(IConfigurationRoot configurationRoot)
        {
            _config = configurationRoot;
            _whiteList = configurationRoot.GetSection("WhiteList").Get<List<string>>();
            _blacklist = new List<Person>();

            var chromeOptions = new ChromeOptions();
            _webdriver = new ChromeDriver(chromeOptions);
            _webdriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            _webdriver.Manage().Window.Maximize();
        }

        public async Task Run()
        {
            await Login();
            await WhoDontMeFollow();
            await UnfollowThese();
            await SaveHistory();
            Dispose();
        }

        private async Task WhoDontMeFollow()
        {
            WebDriverWait wait = new WebDriverWait(_webdriver, TimeSpan.FromSeconds(10));
            IList<IWebElement> tabsLinks = wait.Until(item => _webdriver.FindElement(By.XPath(UlOptions)).FindElements(By.TagName("li")));

            var whoIamFollowing = WhoIamFollowing(wait, tabsLinks);

            tabsLinks = wait.Until(item => _webdriver.FindElement(By.XPath(UlOptions)).FindElements(By.TagName("li")));
            IList<IWebElement> linksParaModalSeguidores = wait.Until(item => tabsLinks.ToArray()[1].FindElements(By.XPath(FollowingModalbtn)));

            // Abrir modal dos seguidores
            linksParaModalSeguidores[0].Click();

            var myfollowers = new List<string>();
            var lap = 1;
            var alreadyAnalizeEveryBody = false;

            while (!alreadyAnalizeEveryBody)
            {
                // buscar os elementos html que contem as pessoas que eu sigo.
                ICollection<IWebElement> lis = wait.Until(item => _webdriver.FindElement(By.XPath(FollowingModal)).FindElements(By.TagName("li")));
                var @as = lis.Select(item => item.FindElement(By.TagName("a"))).ToList();

                @as = @as.Skip(myfollowers.Count()).ToList();

                if (!@as.Any())
                {
                    alreadyAnalizeEveryBody = true;
                    continue;
                }

                // montando a lista de pessoas que eu sigo.
                foreach (var a in @as)
                {
                    var value = a.GetAttribute("innerHTML");

                    if (string.IsNullOrEmpty(value)) continue;

                    if (value.Contains("<img"))
                    {
                        value = value.Split("Foto do perfil de ")[1].Split(" ")[0];
                        value = value.Remove(value.Length - 1);
                    }

                    myfollowers.Add(value);
                }

                _webdriver.ExecuteJavaScript($"document.getElementsByClassName('isgrP')[0].scrollBy(0, 3000)");
                await Task.Delay(1000);
                lap++;
            }

            IList<IWebElement> topoModalDosquemEuSigo = wait.Until(item => _webdriver.FindElement(By.XPath(DivModalSeguindo)).FindElements(By.ClassName("WaOAr")));
            // Fechando modal de quem eu sigo.
            topoModalDosquemEuSigo[1].Click();

           var whoDontMeFollower = whoIamFollowing.Except(myfollowers).ToList();

            _blacklist = whoIamFollowing.Except(_whiteList).Select( item  => new Person { Date = DateTime.Now, Name = item } ).ToList();
        }

        private async Task SaveHistory()
        {
            var path = _config.GetSection("SavePath").Get<string>();

            var filename = Path.Combine(path, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv");

            using (var writer = new StreamWriter($"{filename}"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.Configuration.HasHeaderRecord = true;
                csv.Configuration.AutoMap<Person>();
                csv.WriteRecords(_blacklist);
            }
        }

        private async Task UnfollowThese()
        {
            Console.WriteLine("Deixando de seguir..");

            var removeThese = _blacklist.Select(item => item.Name).ToList();
            WebDriverWait wait = new WebDriverWait(_webdriver, TimeSpan.FromSeconds(10));
            IList<IWebElement> tabsLinks = wait.Until(item => _webdriver.FindElement(By.XPath(UlOptions)).FindElements(By.TagName("li")));
            IList<IWebElement> linksParaModalSeguindo = wait.Until(item => tabsLinks.ToArray()[0].FindElements(By.XPath(FollowingModalbtn)));

            linksParaModalSeguindo[1].Click();

            var quemEuSigo = new List<string>();
            var alreadyAnalizeEveryBody = false;

            while (!alreadyAnalizeEveryBody)
            {
                // buscar os elementos html que contem as pessoas que eu sigo.
                ICollection<IWebElement> lis = wait.Until(item => _webdriver.FindElement(By.XPath(FollowingModal)).FindElements(By.TagName("li")));

                lis = lis.Skip(quemEuSigo.Count()).ToList();

                if (!lis.Any() || !removeThese.Any())
                {
                    alreadyAnalizeEveryBody = true;
                    continue;
                }

                // montando a lista de pessoas que eu sigo.
                foreach (var li in lis)
                {
                    var href = li.FindElement(By.TagName("a"));
                    var button = li.FindElement(By.TagName("button"));

                    if (!removeThese.Any()) break;

                    var value = href.GetAttribute("innerHTML");

                    if (string.IsNullOrEmpty(value)) continue;

                    if (value.Contains("<img"))
                    {
                        value = value.Split("Foto do perfil de ")[1].Split(" ")[0];
                        value = value.Remove(value.Length - 1);
                    }

                    if (removeThese.Any(item => item == value))
                    {

                        button.Click();

                        var btnModal = wait.Until(item => _webdriver.FindElements(By.ClassName(UnFollowBtn)))[0];

                        var html = btnModal.GetAttribute("innerHTML");
                        btnModal.Click();

                        removeThese.Remove(value);
                    }
                }

                Thread.Sleep(1500);
                _webdriver.ExecuteJavaScript($"document.getElementsByClassName('isgrP')[0].scrollBy(0, 3000)");
            }

            IList<IWebElement> topoModalDosquemEuSigo = wait.Until(item => _webdriver.FindElement(By.XPath(DivModalSeguindo)).FindElements(By.ClassName("WaOAr")));
            // Fechando modal de quem eu sigo.
            topoModalDosquemEuSigo[1].Click();
        }

        private async Task Login()
        {
            WebDriverWait wait = new WebDriverWait(_webdriver, TimeSpan.FromSeconds(5));
            var enviorment = _config.GetSection("Enviorment").Get<Enviorment>();

            _webdriver.Navigate().GoToUrl(enviorment.Url);

            IWebElement authFacebook = wait.Until(item => _webdriver.FindElement(By.XPath(AuthFacebook)));
            authFacebook.Click();

            IWebElement inputEmail =  wait.Until(item => _webdriver.FindElement(By.Id(EmailInputId)));
            IWebElement inputpassword = wait.Until(item => _webdriver.FindElement(By.Id(PasswordInputId)));
            IWebElement signinButton = wait.Until(item => _webdriver.FindElement(By.Id(SigInFacebookId)));
            inputEmail.SendKeys(enviorment.Email);
            inputpassword.SendKeys(enviorment.Password);
            signinButton.Click();

            //IWebElement divModalNotification = wait.Until(item => _webdriver.FindElement(By.XPath(BtnCloseModalNotification)));
            //var children = divModalNotification.FindElements(By.ClassName("aOOlW"));
            //children[1].Click();

            IWebElement avatar = wait.Until(item => _webdriver.FindElement(By.XPath(Avatar)));
            avatar.Click();
        }

        public void Dispose()
        {
            _webdriver.Quit();
        }

        private IList<string> WhoIamFollowing(WebDriverWait wait, IList<IWebElement> tabsLinks)
        {
            IList<IWebElement> linksParaModalSeguindo = wait.Until(item => tabsLinks.ToArray()[0].FindElements(By.XPath(FollowingModalbtn)));

            // Abrir modal de quem me segue
            linksParaModalSeguindo[1].Click();

            var quemEuSigo = new List<string>();
            var lap = 1;
            var alreadyAnalizeEveryBody = false;

            while (!alreadyAnalizeEveryBody)
            {
                // buscar os elementos html que contem as pessoas que eu sigo.
                ICollection<IWebElement> lis = wait.Until(item => _webdriver.FindElement(By.XPath(FollowingModal)).FindElements(By.TagName("li")));
                var @as = lis.Select(item => item.FindElement(By.TagName("a"))).ToList();

                @as = @as.Skip(quemEuSigo.Count()).ToList();

                if (!@as.Any())
                {
                    alreadyAnalizeEveryBody = true;
                    continue;
                }

                // montando a lista de pessoas que eu sigo.
                foreach (var a in @as)
                {
                    var value = a.GetAttribute("innerHTML");

                    if (string.IsNullOrEmpty(value)) continue;

                    if (value.Contains("<img"))
                    {
                        value = value.Split("Foto do perfil de ")[1].Split(" ")[0];
                        value = value.Remove(value.Length - 1);
                    }

                    quemEuSigo.Add(value);
                }

                Thread.Sleep(1500);
                _webdriver.ExecuteJavaScript($"document.getElementsByClassName('isgrP')[0].scrollBy(0, 357* { lap })");
                lap++;
            }

            IList<IWebElement> topoModalDosquemEuSigo = wait.Until(item => _webdriver.FindElement(By.XPath(DivModalSeguindo)).FindElements(By.ClassName("WaOAr")));
            // Fechando modal de quem eu sigo.
            topoModalDosquemEuSigo[1].Click();

            return quemEuSigo;
        }
    }
}
