namespace PVSFormat
{
    partial class PVSEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pvsSplitContainer = new System.Windows.Forms.SplitContainer();
            pvsMain = new PVSEditControl();
            deleteZoneButton = new System.Windows.Forms.Button();
            addZoneButton = new System.Windows.Forms.Button();
            zonesListBox = new System.Windows.Forms.ListBox();
            deleteNeighbourButton = new System.Windows.Forms.Button();
            addNeighbourButton = new System.Windows.Forms.Button();
            yLabel2 = new System.Windows.Forms.Label();
            yLabel3 = new System.Windows.Forms.Label();
            yLabel4 = new System.Windows.Forms.Label();
            yLabel1 = new System.Windows.Forms.Label();
            immediateCheckBox = new System.Windows.Forms.CheckBox();
            renderCheckBox = new System.Windows.Forms.CheckBox();
            zonesLabel = new System.Windows.Forms.Label();
            neighboursListBox = new System.Windows.Forms.ListBox();
            neighboursLabel = new System.Windows.Forms.Label();
            xLabel2 = new System.Windows.Forms.Label();
            point2YNumericUpDown = new System.Windows.Forms.NumericUpDown();
            point2XNumericUpDown = new System.Windows.Forms.NumericUpDown();
            xLabel3 = new System.Windows.Forms.Label();
            point3YNumericUpDown = new System.Windows.Forms.NumericUpDown();
            point3XNumericUpDown = new System.Windows.Forms.NumericUpDown();
            xLabel4 = new System.Windows.Forms.Label();
            point4YNumericUpDown = new System.Windows.Forms.NumericUpDown();
            point4XNumericUpDown = new System.Windows.Forms.NumericUpDown();
            xLabel1 = new System.Windows.Forms.Label();
            point1YNumericUpDown = new System.Windows.Forms.NumericUpDown();
            point1XNumericUpDown = new System.Windows.Forms.NumericUpDown();
            pointsLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)pvsSplitContainer).BeginInit();
            pvsSplitContainer.Panel1.SuspendLayout();
            pvsSplitContainer.Panel2.SuspendLayout();
            pvsSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)point2YNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize)point2XNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize)point3YNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize)point3XNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize)point4YNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize)point4XNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize)point1YNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize)point1XNumericUpDown).BeginInit();
            SuspendLayout();

            //
            // pvsSplitContainer
            //
            pvsSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            pvsSplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            pvsSplitContainer.Location = new System.Drawing.Point(0, 0);
            pvsSplitContainer.Name = "pvsSplitContainer";

            //
            // pvsSplitContainer.Panel1
            //
            pvsSplitContainer.Panel1.Controls.Add(pvsMain);

            //
            // pvsSplitContainer.Panel2
            //
            pvsSplitContainer.Panel2.Controls.Add(deleteZoneButton);
            pvsSplitContainer.Panel2.Controls.Add(addZoneButton);
            pvsSplitContainer.Panel2.Controls.Add(zonesListBox);
            pvsSplitContainer.Panel2.Controls.Add(deleteNeighbourButton);
            pvsSplitContainer.Panel2.Controls.Add(addNeighbourButton);
            pvsSplitContainer.Panel2.Controls.Add(yLabel2);
            pvsSplitContainer.Panel2.Controls.Add(yLabel3);
            pvsSplitContainer.Panel2.Controls.Add(yLabel4);
            pvsSplitContainer.Panel2.Controls.Add(yLabel1);
            pvsSplitContainer.Panel2.Controls.Add(immediateCheckBox);
            pvsSplitContainer.Panel2.Controls.Add(renderCheckBox);
            pvsSplitContainer.Panel2.Controls.Add(zonesLabel);
            pvsSplitContainer.Panel2.Controls.Add(neighboursListBox);
            pvsSplitContainer.Panel2.Controls.Add(neighboursLabel);
            pvsSplitContainer.Panel2.Controls.Add(xLabel2);
            pvsSplitContainer.Panel2.Controls.Add(point2YNumericUpDown);
            pvsSplitContainer.Panel2.Controls.Add(point2XNumericUpDown);
            pvsSplitContainer.Panel2.Controls.Add(xLabel3);
            pvsSplitContainer.Panel2.Controls.Add(point3YNumericUpDown);
            pvsSplitContainer.Panel2.Controls.Add(point3XNumericUpDown);
            pvsSplitContainer.Panel2.Controls.Add(xLabel4);
            pvsSplitContainer.Panel2.Controls.Add(point4YNumericUpDown);
            pvsSplitContainer.Panel2.Controls.Add(point4XNumericUpDown);
            pvsSplitContainer.Panel2.Controls.Add(xLabel1);
            pvsSplitContainer.Panel2.Controls.Add(point1YNumericUpDown);
            pvsSplitContainer.Panel2.Controls.Add(point1XNumericUpDown);
            pvsSplitContainer.Panel2.Controls.Add(pointsLabel);
            pvsSplitContainer.Size = new System.Drawing.Size(784, 561);
            pvsSplitContainer.SplitterDistance = 580;
            pvsSplitContainer.TabIndex = 0;

            //
            // pvsMain
            //
            pvsMain.BackColor = System.Drawing.Color.Black;
            pvsMain.Dock = System.Windows.Forms.DockStyle.Fill;
            pvsMain.Font = new System.Drawing.Font("Courier New", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            pvsMain.ForeColor = System.Drawing.Color.White;
            pvsMain.Location = new System.Drawing.Point(0, 0);
            pvsMain.Name = "pvsMain";
            pvsMain.PVS = null;
            pvsMain.Size = new System.Drawing.Size(580, 561);
            pvsMain.TabIndex = 0;
            pvsMain.Click += pvsMain_Click;
            pvsMain.DoubleClick += pvsMain_DoubleClick;

            //
            // deleteZoneButton
            //
            deleteZoneButton.Enabled = false;
            deleteZoneButton.Location = new System.Drawing.Point(131, 8);
            deleteZoneButton.Name = "deleteZoneButton";
            deleteZoneButton.Size = new System.Drawing.Size(56, 23);
            deleteZoneButton.TabIndex = 28;
            deleteZoneButton.Text = "Delete";
            deleteZoneButton.UseVisualStyleBackColor = true;
            deleteZoneButton.Click += deleteZoneButton_Click;

            //
            // addZoneButton
            //
            addZoneButton.Location = new System.Drawing.Point(11, 8);
            addZoneButton.Name = "addZoneButton";
            addZoneButton.Size = new System.Drawing.Size(56, 23);
            addZoneButton.TabIndex = 27;
            addZoneButton.Text = "Add";
            addZoneButton.UseVisualStyleBackColor = true;
            addZoneButton.Click += addZoneButton_Click;

            //
            // zonesListBox
            //
            zonesListBox.FormattingEnabled = true;
            zonesListBox.ItemHeight = 15;
            zonesListBox.Location = new System.Drawing.Point(12, 32);
            zonesListBox.Name = "zonesListBox";
            zonesListBox.Size = new System.Drawing.Size(177, 184);
            zonesListBox.Sorted = true;
            zonesListBox.TabIndex = 26;
            zonesListBox.SelectedIndexChanged += zonesListBox_SelectedIndexChanged;

            //
            // deleteNeighbourButton
            //
            deleteNeighbourButton.Enabled = false;
            deleteNeighbourButton.Location = new System.Drawing.Point(131, 353);
            deleteNeighbourButton.Name = "deleteNeighbourButton";
            deleteNeighbourButton.Size = new System.Drawing.Size(56, 23);
            deleteNeighbourButton.TabIndex = 24;
            deleteNeighbourButton.Text = "Delete";
            deleteNeighbourButton.UseVisualStyleBackColor = true;
            deleteNeighbourButton.Click += deleteNeighbourButton_Click;

            //
            // addNeighbourButton
            //
            addNeighbourButton.Enabled = false;
            addNeighbourButton.Location = new System.Drawing.Point(11, 353);
            addNeighbourButton.Name = "addNeighbourButton";
            addNeighbourButton.Size = new System.Drawing.Size(56, 23);
            addNeighbourButton.TabIndex = 23;
            addNeighbourButton.Text = "Add";
            addNeighbourButton.UseVisualStyleBackColor = true;
            addNeighbourButton.Click += addNeighbourButton_Click;

            //
            // yLabel2
            //
            yLabel2.AutoSize = true;
            yLabel2.Location = new System.Drawing.Point(100, 269);
            yLabel2.Name = "yLabel2";
            yLabel2.Size = new System.Drawing.Size(13, 15);
            yLabel2.TabIndex = 22;
            yLabel2.Text = "y";

            //
            // yLabel3
            //
            yLabel3.AutoSize = true;
            yLabel3.Location = new System.Drawing.Point(100, 298);
            yLabel3.Name = "yLabel3";
            yLabel3.Size = new System.Drawing.Size(13, 15);
            yLabel3.TabIndex = 21;
            yLabel3.Text = "y";

            //
            // yLabel4
            //
            yLabel4.AutoSize = true;
            yLabel4.Location = new System.Drawing.Point(100, 327);
            yLabel4.Name = "yLabel4";
            yLabel4.Size = new System.Drawing.Size(13, 15);
            yLabel4.TabIndex = 20;
            yLabel4.Text = "y";

            //
            // yLabel1
            //
            yLabel1.AutoSize = true;
            yLabel1.Location = new System.Drawing.Point(100, 240);
            yLabel1.Name = "yLabel1";
            yLabel1.Size = new System.Drawing.Size(13, 15);
            yLabel1.TabIndex = 19;
            yLabel1.Text = "y";

            //
            // immediateCheckBox
            //
            immediateCheckBox.AutoSize = true;
            immediateCheckBox.Enabled = false;
            immediateCheckBox.Location = new System.Drawing.Point(97, 538);
            immediateCheckBox.Name = "immediateCheckBox";
            immediateCheckBox.Size = new System.Drawing.Size(83, 19);
            immediateCheckBox.TabIndex = 18;
            immediateCheckBox.Text = "Immediate";
            immediateCheckBox.UseVisualStyleBackColor = true;
            immediateCheckBox.CheckedChanged += immediateCheckBox_CheckedChanged;

            //
            // renderCheckBox
            //
            renderCheckBox.AutoSize = true;
            renderCheckBox.Enabled = false;
            renderCheckBox.Location = new System.Drawing.Point(23, 538);
            renderCheckBox.Name = "renderCheckBox";
            renderCheckBox.Size = new System.Drawing.Size(63, 19);
            renderCheckBox.TabIndex = 17;
            renderCheckBox.Text = "Render";
            renderCheckBox.UseVisualStyleBackColor = true;
            renderCheckBox.CheckedChanged += renderCheckBox_CheckedChanged;

            //
            // zonesLabel
            //
            zonesLabel.AutoSize = true;
            zonesLabel.Location = new System.Drawing.Point(79, 12);
            zonesLabel.Name = "zonesLabel";
            zonesLabel.Size = new System.Drawing.Size(39, 15);
            zonesLabel.TabIndex = 15;
            zonesLabel.Text = "Zones";

            //
            // neighboursListBox
            //
            neighboursListBox.Enabled = false;
            neighboursListBox.FormattingEnabled = true;
            neighboursListBox.ItemHeight = 15;
            neighboursListBox.Location = new System.Drawing.Point(11, 378);
            neighboursListBox.Name = "neighboursListBox";
            neighboursListBox.Size = new System.Drawing.Size(177, 154);
            neighboursListBox.Sorted = true;
            neighboursListBox.TabIndex = 14;
            neighboursListBox.SelectedIndexChanged += neighboursListBox_SelectedIndexChanged;

            //
            // neighboursLabel
            //
            neighboursLabel.AutoSize = true;
            neighboursLabel.Location = new System.Drawing.Point(65, 357);
            neighboursLabel.Name = "neighboursLabel";
            neighboursLabel.Size = new System.Drawing.Size(69, 15);
            neighboursLabel.TabIndex = 13;
            neighboursLabel.Text = "Neighbours";

            //
            // xLabel2
            //
            xLabel2.AutoSize = true;
            xLabel2.Location = new System.Drawing.Point(10, 269);
            xLabel2.Name = "xLabel2";
            xLabel2.Size = new System.Drawing.Size(13, 15);
            xLabel2.TabIndex = 12;
            xLabel2.Text = "x";

            //
            // point2YNumericUpDown
            //
            point2YNumericUpDown.DecimalPlaces = 3;
            point2YNumericUpDown.Enabled = false;
            point2YNumericUpDown.Location = new System.Drawing.Point(115, 265);
            point2YNumericUpDown.Maximum = new decimal(new int[]
            {
                50000,
                0,
                0,
                0
            });
            point2YNumericUpDown.Minimum = new decimal(new int[]
            {
                50000,
                0,
                0,
                int.MinValue
            });
            point2YNumericUpDown.Name = "point2YNumericUpDown";
            point2YNumericUpDown.Size = new System.Drawing.Size(70, 23);
            point2YNumericUpDown.TabIndex = 11;
            point2YNumericUpDown.ValueChanged += point2YNumericUpDown_ValueChanged;

            //
            // point2XNumericUpDown
            //
            point2XNumericUpDown.DecimalPlaces = 3;
            point2XNumericUpDown.Enabled = false;
            point2XNumericUpDown.Location = new System.Drawing.Point(24, 265);
            point2XNumericUpDown.Maximum = new decimal(new int[]
            {
                50000,
                0,
                0,
                0
            });
            point2XNumericUpDown.Minimum = new decimal(new int[]
            {
                50000,
                0,
                0,
                int.MinValue
            });
            point2XNumericUpDown.Name = "point2XNumericUpDown";
            point2XNumericUpDown.Size = new System.Drawing.Size(70, 23);
            point2XNumericUpDown.TabIndex = 10;
            point2XNumericUpDown.ValueChanged += point2XNumericUpDown_ValueChanged;

            //
            // xLabel3
            //
            xLabel3.AutoSize = true;
            xLabel3.Location = new System.Drawing.Point(10, 298);
            xLabel3.Name = "xLabel3";
            xLabel3.Size = new System.Drawing.Size(13, 15);
            xLabel3.TabIndex = 9;
            xLabel3.Text = "x";

            //
            // point3YNumericUpDown
            //
            point3YNumericUpDown.DecimalPlaces = 3;
            point3YNumericUpDown.Enabled = false;
            point3YNumericUpDown.Location = new System.Drawing.Point(115, 294);
            point3YNumericUpDown.Maximum = new decimal(new int[]
            {
                50000,
                0,
                0,
                0
            });
            point3YNumericUpDown.Minimum = new decimal(new int[]
            {
                50000,
                0,
                0,
                int.MinValue
            });
            point3YNumericUpDown.Name = "point3YNumericUpDown";
            point3YNumericUpDown.Size = new System.Drawing.Size(70, 23);
            point3YNumericUpDown.TabIndex = 8;
            point3YNumericUpDown.ValueChanged += point3YNumericUpDown_ValueChanged;

            //
            // point3XNumericUpDown
            //
            point3XNumericUpDown.DecimalPlaces = 3;
            point3XNumericUpDown.Enabled = false;
            point3XNumericUpDown.Location = new System.Drawing.Point(24, 294);
            point3XNumericUpDown.Maximum = new decimal(new int[]
            {
                50000,
                0,
                0,
                0
            });
            point3XNumericUpDown.Minimum = new decimal(new int[]
            {
                50000,
                0,
                0,
                int.MinValue
            });
            point3XNumericUpDown.Name = "point3XNumericUpDown";
            point3XNumericUpDown.Size = new System.Drawing.Size(70, 23);
            point3XNumericUpDown.TabIndex = 7;
            point3XNumericUpDown.ValueChanged += point3XNumericUpDown_ValueChanged;

            //
            // xLabel4
            //
            xLabel4.AutoSize = true;
            xLabel4.Location = new System.Drawing.Point(10, 327);
            xLabel4.Name = "xLabel4";
            xLabel4.Size = new System.Drawing.Size(13, 15);
            xLabel4.TabIndex = 6;
            xLabel4.Text = "x";

            //
            // point4YNumericUpDown
            //
            point4YNumericUpDown.DecimalPlaces = 3;
            point4YNumericUpDown.Enabled = false;
            point4YNumericUpDown.Location = new System.Drawing.Point(115, 323);
            point4YNumericUpDown.Maximum = new decimal(new int[]
            {
                50000,
                0,
                0,
                0
            });
            point4YNumericUpDown.Minimum = new decimal(new int[]
            {
                50000,
                0,
                0,
                int.MinValue
            });
            point4YNumericUpDown.Name = "point4YNumericUpDown";
            point4YNumericUpDown.Size = new System.Drawing.Size(70, 23);
            point4YNumericUpDown.TabIndex = 5;
            point4YNumericUpDown.ValueChanged += point4YNumericUpDown_ValueChanged;

            //
            // point4XNumericUpDown
            //
            point4XNumericUpDown.DecimalPlaces = 3;
            point4XNumericUpDown.Enabled = false;
            point4XNumericUpDown.Location = new System.Drawing.Point(24, 323);
            point4XNumericUpDown.Maximum = new decimal(new int[]
            {
                50000,
                0,
                0,
                0
            });
            point4XNumericUpDown.Minimum = new decimal(new int[]
            {
                50000,
                0,
                0,
                int.MinValue
            });
            point4XNumericUpDown.Name = "point4XNumericUpDown";
            point4XNumericUpDown.Size = new System.Drawing.Size(70, 23);
            point4XNumericUpDown.TabIndex = 4;
            point4XNumericUpDown.ValueChanged += point4XNumericUpDown_ValueChanged;

            //
            // xLabel1
            //
            xLabel1.AutoSize = true;
            xLabel1.Location = new System.Drawing.Point(10, 240);
            xLabel1.Name = "xLabel1";
            xLabel1.Size = new System.Drawing.Size(13, 15);
            xLabel1.TabIndex = 3;
            xLabel1.Text = "x";

            //
            // point1YNumericUpDown
            //
            point1YNumericUpDown.DecimalPlaces = 3;
            point1YNumericUpDown.Enabled = false;
            point1YNumericUpDown.Location = new System.Drawing.Point(115, 236);
            point1YNumericUpDown.Maximum = new decimal(new int[]
            {
                50000,
                0,
                0,
                0
            });
            point1YNumericUpDown.Minimum = new decimal(new int[]
            {
                50000,
                0,
                0,
                int.MinValue
            });
            point1YNumericUpDown.Name = "point1YNumericUpDown";
            point1YNumericUpDown.Size = new System.Drawing.Size(70, 23);
            point1YNumericUpDown.TabIndex = 2;
            point1YNumericUpDown.ValueChanged += point1YNumericUpDown_ValueChanged;

            //
            // point1XNumericUpDown
            //
            point1XNumericUpDown.DecimalPlaces = 3;
            point1XNumericUpDown.Enabled = false;
            point1XNumericUpDown.Location = new System.Drawing.Point(24, 236);
            point1XNumericUpDown.Maximum = new decimal(new int[]
            {
                50000,
                0,
                0,
                0
            });
            point1XNumericUpDown.Minimum = new decimal(new int[]
            {
                50000,
                0,
                0,
                int.MinValue
            });
            point1XNumericUpDown.Name = "point1XNumericUpDown";
            point1XNumericUpDown.Size = new System.Drawing.Size(70, 23);
            point1XNumericUpDown.TabIndex = 1;
            point1XNumericUpDown.ValueChanged += point1XNumericUpDown_ValueChanged;

            //
            // pointsLabel
            //
            pointsLabel.AutoSize = true;
            pointsLabel.Location = new System.Drawing.Point(78, 218);
            pointsLabel.Name = "pointsLabel";
            pointsLabel.Size = new System.Drawing.Size(40, 15);
            pointsLabel.TabIndex = 0;
            pointsLabel.Text = "Points";

            //
            // PVSEditor
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(784, 561);
            Controls.Add(pvsSplitContainer);
            Name = "PVSEditor";
            Text = "PVS Editor";
            FormClosing += PVSEditor_FormClosing;
            pvsSplitContainer.Panel1.ResumeLayout(false);
            pvsSplitContainer.Panel2.ResumeLayout(false);
            pvsSplitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pvsSplitContainer).EndInit();
            pvsSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)point2YNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)point2XNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)point3YNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)point3XNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)point4YNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)point4XNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)point1YNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize)point1XNumericUpDown).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.SplitContainer pvsSplitContainer;
        private System.Windows.Forms.Label pointsLabel;
        private PVSEditControl pvsMain;
        private System.Windows.Forms.Label xLabel2;
        private System.Windows.Forms.Label xLabel3;
        private System.Windows.Forms.Label xLabel4;
        private System.Windows.Forms.Label xLabel1;
        private System.Windows.Forms.Label neighboursLabel;
        private System.Windows.Forms.Label zonesLabel;
        private System.Windows.Forms.NumericUpDown point1XNumericUpDown;
        private System.Windows.Forms.NumericUpDown point1YNumericUpDown;
        private System.Windows.Forms.NumericUpDown point2YNumericUpDown;
        private System.Windows.Forms.NumericUpDown point2XNumericUpDown;
        private System.Windows.Forms.NumericUpDown point3YNumericUpDown;
        private System.Windows.Forms.NumericUpDown point3XNumericUpDown;
        private System.Windows.Forms.NumericUpDown point4YNumericUpDown;
        private System.Windows.Forms.NumericUpDown point4XNumericUpDown;
        private System.Windows.Forms.ListBox neighboursListBox;
        private System.Windows.Forms.CheckBox immediateCheckBox;
        private System.Windows.Forms.CheckBox renderCheckBox;
        private System.Windows.Forms.Label yLabel2;
        private System.Windows.Forms.Label yLabel3;
        private System.Windows.Forms.Label yLabel4;
        private System.Windows.Forms.Label yLabel1;
        private System.Windows.Forms.Button deleteNeighbourButton;
        private System.Windows.Forms.Button addNeighbourButton;
        private System.Windows.Forms.Button deleteZoneButton;
        private System.Windows.Forms.Button addZoneButton;
        private System.Windows.Forms.ListBox zonesListBox;
    }
}
