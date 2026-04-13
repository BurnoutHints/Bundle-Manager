using BundleUtilities;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace BundleUtilities
{
    public partial class LoadingDialog : Form, ILoader
    {
        public delegate void OnDone(bool cancelled, object value);
        public event OnDone Done;

        private delegate object GetObject();
        private delegate void SetObject(object obj);
        private object _value;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public object Value
        {
            get
            {
                if (InvokeRequired)
                {
                    GetObject method = () =>
                    {
                        return _value;
                    };
                    return Invoke(method);
                }
                else
                {
                    return _value;
                }
            }
            set
            {
                if (InvokeRequired)
                {
                    SetObject method = (object obj) =>
                    {
                        _value = obj;
                    };
                    Invoke(method, value);
                }
                else
                {
                    _value = value;
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool IsDone { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Status
        {
            get => lblStatus.Text;
            set => lblStatus.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int Progress
        {
            get => pboMain.Value;
            set => pboMain.Value = value;
        }

        public LoadingDialog()
        {
            InitializeComponent();

            IsDone = false;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void LoadingDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            Done?.Invoke(!IsDone, Value);
        }

        private void tmrDoneCheck_Tick(object sender, EventArgs e)
        {
            if (IsDone)
            {
                tmrDoneCheck.Enabled = false;
                Close();
            }
        }

        private delegate void SetStatusDelegate(string status);
        public void SetStatus(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new SetStatusDelegate(SetStatus), status);
            }
            else
            {
                if (Status != status)
                    Status = status;
            }
        }

        private delegate void SetProgressDelegate(int progress);
        public void SetProgress(int progress)
        {
            if (InvokeRequired)
            {
                Invoke(new SetProgressDelegate(SetProgress), progress);
            }
            else
            {
                if (Progress != progress)
                    Progress = progress;
                if (pboMain.Style == ProgressBarStyle.Marquee)
                {
                    pboMain.Style = ProgressBarStyle.Blocks;
                }
            }
        }
    }
}
