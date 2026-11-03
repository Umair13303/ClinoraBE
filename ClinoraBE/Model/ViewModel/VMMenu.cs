namespace ClinoraBE.Model.ViewModel
{
    public class VMMenu
    {
        public string Menu { get; set; }
        public List<VMSubMenu> SubMenu { get; set; } = new();
    }

    public class VMSubMenu
    {
        public string SubMenu { get; set; }
        public List<VMRight> Rights { get; set; } = new();
    }

    public class VMRight
    {
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public string OperationType { get; set; }
        public int Priority { get; set; }
    }
}
