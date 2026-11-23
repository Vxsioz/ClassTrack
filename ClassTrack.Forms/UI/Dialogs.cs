using ClassTrack.UI.Controls;

namespace ClassTrack.UI
{
    /// <summary>Themed replacements for MessageBox.</summary>
    public static class Dialogs
    {
        public static void Info(IWin32Window? owner, string title, string message)
        {
            using var dlg = new MessageDialog(title, message, "OK", null, false);
            dlg.ShowDialog(owner);
        }

        public static void Error(IWin32Window? owner, string title, string message)
        {
            using var dlg = new MessageDialog(title, message, "OK", null, false);
            dlg.ShowDialog(owner);
        }

        /// <summary>Returns true if the user pressed the confirm button.</summary>
        public static bool Confirm(IWin32Window? owner, string title, string message, string confirmText = "Delete", bool danger = true)
        {
            using var dlg = new MessageDialog(title, message, confirmText, "Cancel", danger);
            return dlg.ShowDialog(owner) == DialogResult.OK;
        }
    }

    /// <summary>A small glass sheet over the ambient backdrop: heading, message, one or two buttons.</summary>
    internal sealed class MessageDialog : DarkForm
    {
        public MessageDialog(string title, string message, string okText, string? cancelText, bool danger)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(14);

            var card = new GlassPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(28, 26, 28, 22), Radius = 22 };
            var layout = new TablePanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                MinimumSize = new Size(360, 0)
            };

            var heading = Ui.Text(title, Theme.H2, Theme.Text, new Padding(0, 0, 0, 8));
            var body = Ui.Text(message, Theme.Body, Theme.TextMuted, new Padding(0, 0, 0, 24));
            body.MaximumSize = new Size(440, 0);

            var ok = new FlatButton(okText, danger ? ButtonKind.Danger : ButtonKind.Primary)
            {
                DialogResult = DialogResult.OK,
                Margin = new Padding(10, 0, 0, 0)
            };
            var buttons = new FlowPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            buttons.Controls.Add(ok);
            AcceptButton = ok;
            CancelButton = ok;

            if (cancelText != null)
            {
                var cancel = new FlatButton(cancelText) { DialogResult = DialogResult.Cancel };
                buttons.Controls.Add(cancel);
                CancelButton = cancel;
                if (danger)
                {
                    // Enter must never confirm a destructive action: default AND initial focus go to Cancel
                    // (a focused button would otherwise win over the dialog's default button).
                    AcceptButton = cancel;
                    Shown += (_, _) => cancel.Focus();
                }
            }

            layout.Controls.Add(heading, 0, 0);
            layout.Controls.Add(body, 0, 1);
            layout.Controls.Add(buttons, 0, 2);
            card.Controls.Add(layout);
            Controls.Add(card);
        }
    }
}
