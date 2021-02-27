using System.Windows.Forms;
using System.Collections;
using System.IO;
using System.ComponentModel;
using System.Xml;

namespace ArmaServerCmdUtil
{
    public partial class MainWindow : Form
    {
        private class FileNameFromPath
        {
            public string Path;
            public string Name
            {
                get
                {
                    return System.IO.Path.GetFileNameWithoutExtension(Path);
                }
            }

            public FileNameFromPath(string _path)
            {
                Path = _path;
            }
        }

        private class RecentFileToolStripItem : ToolStripMenuItem
        {
            public string FilePath;
        }

        private class TitleProperty : System.ComponentModel.INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private string _Title = "";
            public string Title { get { return _Title; } }

            public void UpdateTitle(FileNameFromPath currentPreset, bool presetDirty)
            {
                _Title = "Arma Server Cmd Util";
                if (currentPreset != null)
                {
                    _Title += " - " + currentPreset.Name;
                    if (presetDirty)
                    {
                        _Title += "*";
                    }
                }
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("text"));
            }
        }

        private string cmdPrefix;
        private ArrayList modNames;
        private ArrayList recentPresets;
        private FileNameFromPath currentPreset;
        private bool presetDirty = false;
        private TitleProperty titleProperty;
        private BindingSource titleBindingSource;
        private bool inLoad = false;

        public MainWindow()
        {
            InitializeComponent();

            modNames = new ArrayList();
            recentPresets = new ArrayList();

            titleProperty = new TitleProperty();
            titleBindingSource = new BindingSource();
            titleBindingSource.DataSource = titleProperty;

            DataBindings.Add("text", titleBindingSource, "Title", false, DataSourceUpdateMode.OnPropertyChanged);

            generalTooltip.SetToolTip(CopyToClipboard, "Copy to clipboard");
            generalTooltip.SetToolTip(addModBtn, "Add mod to list");
            generalTooltip.SetToolTip(removeModBtn, "Remove selected mod from list");
        }

        private void CopyToClipboard_Click(object sender, System.EventArgs e)
        {
            Clipboard.SetText(out_cmd.Text);
        }

        private void newMenu_Click(object sender, System.EventArgs e)
        {
            if(!CheckForNeededSave())
            {
                return;
            }
            inLoad = true;

            for (int i = 0; i < modList.Items.Count; ++i)
            {
                modList.SetItemChecked(i, false);
            }
            currentPreset = null;
            presetDirty = false;

            UpdateTitle();
            UpdateOutputLine();
            SaveConfig();

            inLoad = false;
        }

        private void saveMenu_Click(object sender, System.EventArgs e)
        {
            if (currentPreset == null)
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter = "Mod Preset|*.modPreset";
                saveDialog.Title = "Save";
                saveDialog.ShowDialog();

                SavePreset(saveDialog.FileName);
            }
            else
            {
                SavePreset(currentPreset.Path);
            }
        }

        private void saveAsMenu_Click(object sender, System.EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Mod Preset|*.modPreset";
            saveDialog.Title = "Save as";
            saveDialog.ShowDialog();

            SavePreset(saveDialog.FileName);
        }

        private void loadMenu_Click(object sender, System.EventArgs e)
        {
            if (!CheckForNeededSave())
            {
                return;
            }
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Mod Preset|*.modPreset";
            openDialog.Title = "Open";
            openDialog.ShowDialog();

            if (openDialog.FileName.Length != 0)
            {
                LoadPreset(openDialog.FileName);
            }
        }

        private void recentPresetsMenu_Click(object sender, System.EventArgs e)
        {
            if (!CheckForNeededSave())
            {
                return;
            }
            if (sender is RecentFileToolStripItem)
            {
                RecentFileToolStripItem recentFileToolStripItem = (RecentFileToolStripItem)sender;
                LoadPreset(recentFileToolStripItem.FilePath);
            }
        }

        private void quitMenu_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void modList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if(inLoad)
            {
                return;
            }
            //this is called before the set of checked items is updated
            //so defer the update later so that it happens after the item checked lsit is updated
            BeginInvoke((MethodInvoker)(() =>
            {
                UpdateOutputLine();
                presetDirty = true;
                UpdateTitle();
            }));
        }

        private void addModBtn_Click(object sender, System.EventArgs e)
        {
            AddMod();            
        }

        private void removeModBtn_Click(object sender, System.EventArgs e)
        {
            RemoveSelectedMod();
        }
        private void cmdPrefixMenu_Click(object sender, System.EventArgs e)
        {
            PrefixEntry prefixEntryForm = new PrefixEntry(cmdPrefix);
            if(prefixEntryForm.ShowDialog() == DialogResult.OK)
            {
                cmdPrefix = prefixEntryForm.GetPrefix();
                UpdateOutputLine();
                presetDirty = true;
                UpdateTitle();
            }
        }
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (modNameInput.Focused)
                {
                    AddMod();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.Delete)
            {
                RemoveSelectedMod();
                if (modList.Focused)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void SaveConfig()
        {
            Properties.Settings.Default.Prefix = cmdPrefix;
            Properties.Settings.Default.Mods = new System.Collections.Specialized.StringCollection();
            foreach (string modName in modNames)
            {
                Properties.Settings.Default.Mods.Add(modName);
            }

            Properties.Settings.Default.RecentPresets = new System.Collections.Specialized.StringCollection();
            foreach (FileNameFromPath recentPreset in recentPresets)
            {
                Properties.Settings.Default.RecentPresets.Add(recentPreset.Path);
            }

            Properties.Settings.Default.LastPreset = currentPreset == null ? "" : currentPreset.Path;

            Properties.Settings.Default.Save();
        }

        private void MainWindow_Load(object sender, System.EventArgs e)
        {
            cmdPrefix = Properties.Settings.Default.Prefix;
            if (Properties.Settings.Default.Mods != null)
            {
                foreach (string modName in Properties.Settings.Default.Mods)
                {
                    modNames.Add(modName);
                }
                modNames.Sort();
            }

            if (Properties.Settings.Default.RecentPresets == null || Properties.Settings.Default.RecentPresets.Count == 0)
            {
                recentPresets = new ArrayList();
            }
            else
            {
                recentPresets = new ArrayList();
                foreach (string recentPreset in Properties.Settings.Default.RecentPresets)
                {
                    recentPresets.Add(new FileNameFromPath(recentPreset));
                }
            }

            UpdateModList();

            if (Properties.Settings.Default.LastPreset.Length != 0)
            {
                LoadPreset(Properties.Settings.Default.LastPreset);
            }

            UpdateOutputLine();
            UpdateRecentPresets();
            UpdateTitle();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!CheckForNeededSave())
            {
                e.Cancel = true;
            }
        }

        private void SavePreset(string fileName)
        {
            if(fileName == "")
            {
                return;
            }
            try
            {
                XmlDocument presetDoc = new XmlDocument();

                XmlElement rootXML = presetDoc.CreateElement("Save");
                presetDoc.AppendChild(rootXML);

                XmlElement prefixXML = presetDoc.CreateElement("Prefix");
                prefixXML.InnerText = cmdPrefix;
                rootXML.AppendChild(prefixXML);

                XmlElement modListXML = presetDoc.CreateElement("Mods");
                foreach(string modName in modList.CheckedItems)
                {
                    XmlElement modXML = presetDoc.CreateElement("Mod");
                    modXML.InnerText = modName;
                    modListXML.AppendChild(modXML);
                }
                rootXML.AppendChild(modListXML);

                presetDoc.Save(fileName);

                //make sure this is last in case of an exception
                currentPreset = new FileNameFromPath(fileName);
                presetDirty = false;

                UpdateTitle();
                AddRecentPreset(fileName);
                SaveConfig();
            }
            catch
            {
                MessageBox.Show("Preset save error: Unknown reason", "Preset Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPreset(string fileName)
        {
            try
            {
                inLoad = true;
                XmlDocument presetDoc = new XmlDocument();
                presetDoc.Load(fileName);
                
                XmlElement rootXMl = presetDoc["Save"];
                if (rootXMl == null) throw new XmlException("Failed to find root element \"Save\"");

                XmlElement prefixXMl = rootXMl["Prefix"];
                if (prefixXMl == null) throw new XmlException("Failed to find element \"Prefix\"");
                string prefix = prefixXMl.InnerText;
                ArrayList mods = new ArrayList();
                XmlElement modsXML = rootXMl["Mods"];
                if (modsXML == null) throw new XmlException("Failed to find element \"Mods\"");
                foreach (XmlElement modNode in modsXML)
                {
                    mods.Add(modNode.InnerText);
                }

                //xml load finished

                cmdPrefix = prefix;
                for (int i = 0; i < modList.Items.Count; ++i)
                {
                    modList.SetItemChecked(i, false);
                }
                foreach (string mod in mods)
                {
                    int i = 0;
                    for (; i < modList.Items.Count; ++i)
                    {
                        string listedMod = (string)modList.Items[i];
                        if(listedMod == mod)
                        {
                            modList.SetItemChecked(i, true);
                            break;
                        }
                    }
                    if (i == modList.Items.Count)
                    {
                        //failed to find mod
                        MessageBox.Show(string.Format("Failed to load mod \"{0}\" from preset \"{1}\"\nIt will not be included", mod, fileName), "Preset Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                currentPreset = new FileNameFromPath(fileName);
                presetDirty = false;

                UpdateTitle();
                UpdateOutputLine();
                AddRecentPreset(fileName);
                SaveConfig();
            }
            catch(FileNotFoundException)
            {
                //fine, do nothing
                return;
            }
            catch(XmlException ex)
            {
                MessageBox.Show(string.Format("Failed to load preset \"{0}\"\nXML Parse Error(Line:{2},Col:{3}):\n{1}", fileName, ex.Message, ex.LineNumber, ex.LinePosition), "Preset Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                MessageBox.Show("Preset load error: Unknown reason", "Preset Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                inLoad = false;
            }
        }

        private void UpdateModList()
        {
            ArrayList oldCheckedList = new ArrayList();
            foreach(string checkedItem in modList.CheckedItems)
            {
                oldCheckedList.Add(checkedItem);
            }
            modList.Items.Clear();
            foreach (string modName in modNames)
            {
                modList.Items.Add(modName);
            }            

            foreach (string oldCheckedItem in oldCheckedList)
            {
                int checkedIndex = modNames.IndexOf(oldCheckedItem);
                if (checkedIndex != -1)
                {
                    modList.SetItemChecked(checkedIndex, true);
                }
            }
        }
        private void UpdateOutputLine()
        {
            out_cmd.Text = cmdPrefix + " ";
            out_cmd.Text += "-mod=\"";
            foreach(int checkedIndex in modList.CheckedIndices)
            {
                out_cmd.Text += "mods/@" + modNames[checkedIndex] + ";";
            }
            out_cmd.Text += "\"";
        }

        private void UpdateRecentPresets()
        {
            recentPresetsMenu.DropDownItems.Clear();
            if (recentPresets == null || recentPresets.Count == 0)
            {
                ToolStripItem noneMenuItem = new ToolStripMenuItem();
                noneMenuItem.Name = "recentPresetsMenuNone";
                noneMenuItem.Size = new System.Drawing.Size(186, 22);
                noneMenuItem.Text = "None";
                noneMenuItem.Enabled = false;
                recentPresetsMenu.DropDownItems.Add(noneMenuItem);
            }
            else
            {
                foreach (FileNameFromPath recentPreset in recentPresets)
                {
                    RecentFileToolStripItem recentMenuItem = new RecentFileToolStripItem();
                    recentMenuItem.Name = "recentPresetsRecent_" + recentPreset.Path;
                    recentMenuItem.Size = new System.Drawing.Size(186, 22);
                    recentMenuItem.Text = recentPreset.Name;
                    recentMenuItem.Click += recentPresetsMenu_Click;
                    recentMenuItem.FilePath = recentPreset.Path;
                    recentPresetsMenu.DropDownItems.Add(recentMenuItem);
                }
            }
        }

        private void AddMod()
        {
            string newMod = modNameInput.Text;
            if (newMod.Length == 0)
            {
                return;
            }

            foreach (string modName in modNames)
            {
                if (modName == newMod)
                {
                    //already added this mod
                    return;
                }
            }

            modNames.Add(newMod);
            modNameInput.Text = "";
            modNames.Sort();
            UpdateModList();
            SaveConfig();
        }
        private void RemoveSelectedMod()
        {
            int selectedIndex = modList.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < modNames.Count)
            {
                modNames.RemoveAt(selectedIndex);
                UpdateModList();
                UpdateOutputLine();
                SaveConfig();
            }
        }

        private void UpdateTitle()
        {
            titleProperty.UpdateTitle(currentPreset, presetDirty);
        }

        private void AddRecentPreset(string fileName)
        {
            //make sure this file is on top of the recent files
            for (int i = 0; i < recentPresets.Count; ++i)
            {
                if (((FileNameFromPath)recentPresets[i]).Path == fileName)
                {
                    recentPresets.RemoveAt(i);
                    break;
                }
            }
            recentPresets.Insert(0, new FileNameFromPath(fileName));
            UpdateRecentPresets();
        }

        //returns true if we can continue
        //returns false if we want to cancel the current action
        private bool CheckForNeededSave()
        {
            if (!presetDirty)
            {
                return true;
            }

            UnsavedChangesForm unsavedChanges = new UnsavedChangesForm();
            switch(unsavedChanges.ShowDialog())
            {
                case DialogResult.Yes:
                    saveMenu.PerformClick();
                    return true;
                case DialogResult.No:
                    //don't want to save
                    return true;
                case DialogResult.Cancel:
                    //cancel whatever action is in progress
                    return false;
                default:
                    //???? how did we get here
                    return true;
            }
        }
    }
}
