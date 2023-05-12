namespace Twibbly.Shared
{
    public partial class MainLayout
    {
        bool _menuOpen = true;


        public void MenuToggle()
        {
            _menuOpen = !_menuOpen;
        }
    }
}
