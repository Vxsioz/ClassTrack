namespace ClassTrack.UI
{
    /// <summary>Base for every window: dark background, standard font, dark title bar.</summary>
    public class DarkForm : Form
    {
        public DarkForm()
        {
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.Body;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.ApplyDarkTitleBar(Handle);
        }
    }
}
