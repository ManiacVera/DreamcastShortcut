namespace DreamcastShortcutApp.DTO.Info
{
    public class ComboBoxInfo
    {
        public string Text { get; set; }
        public string Value { get; set; }

        public ComboBoxInfo(string text, string value)
        {
            Text = text;
            Value = value;
        }
    }
}
