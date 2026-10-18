using System.Drawing;
using System.Windows.Forms;

namespace BlackCatPomodoro
{
    /// <summary>
    /// Applies a font to a control tree. WinForms does not propagate a form
    /// font to controls that have an explicit font, so the UI font is applied
    /// recursively to every child and tool strip item.
    /// </summary>
    public static class FontHelper
    {
        public static void Apply(Control root, Font font, Control skip = null)
        {
            if (root == null || font == null) return;

            root.Font = font;
            ApplyRecursive(root, font, skip);
        }

        private static void ApplyRecursive(Control parent, Font font, Control skip)
        {
            foreach (Control child in parent.Controls)
            {
                if (child == skip) continue;

                child.Font = font;

                if (child is ToolStrip strip)
                    ApplyToolStrip(strip.Items, font);

                ApplyRecursive(child, font, skip);
            }
        }

        public static void ApplyToolStrip(ToolStripItemCollection items, Font font)
        {
            foreach (ToolStripItem item in items)
            {
                item.Font = font;

                if (item is ToolStripMenuItem menuItem)
                    ApplyToolStrip(menuItem.DropDownItems, font);
            }
        }
    }
}
