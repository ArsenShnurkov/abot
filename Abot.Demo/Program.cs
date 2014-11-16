namespace Abot.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure ();

            //focus_kontur_ru.Crawl ();
            www_list_org_com.Crawl ();
        }
    }

}
