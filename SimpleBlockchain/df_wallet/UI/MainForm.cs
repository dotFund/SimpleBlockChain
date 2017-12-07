using System.Windows.Forms;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.IO;

using df_wallet;
using SimpleBlockchain.Properties;

namespace SimpleBlockchain.UI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        public MainForm(XDocument xdoc = null)
        {
            InitializeComponent();
            /*
            if (xdoc != null)
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                Version latest = Version.Parse(xdoc.Element("update").Attribute("latest").Value);
                if (version < latest)
                {
                    toolStripStatusLabel3.Tag = xdoc;
                    toolStripStatusLabel3.Text += $": {latest}";
                    toolStripStatusLabel3.Visible = true;
                }
            }
            */
        }

        private void MainForm_Load(object sender, System.EventArgs e)
        {
            Task.Run(() =>
            {
                /*
                const string acc_path = "chain.acc";
                const string acc_zip_path = acc_path + ".zip";
                if (File.Exists(acc_path))
                {
                    using (FileStream fs = new FileStream(acc_path, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        ImportBlocks(fs);
                    }
                    File.Delete(acc_path);
                }
                else if (File.Exists(acc_zip_path))
                {
                    using (FileStream fs = new FileStream(acc_zip_path, FileMode.Open, FileAccess.Read, FileShare.None))
                    using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
                    using (Stream zs = zip.GetEntry(acc_path).Open())
                    {
                        ImportBlocks(zs);
                    }
                    File.Delete(acc_zip_path);
                }
                Blockchain.PersistCompleted += Blockchain_PersistCompleted;
                */
                Program.LocalNode.Start(Settings.Default.NodePort, Settings.Default.WsPort);
            });
        }

        private void timer1_Tick(object sender, System.EventArgs e)
        {
            tss_lbl_connected_value.Text = Program.LocalNode.RemoteNodeCount.ToString();
        }
    }
}
