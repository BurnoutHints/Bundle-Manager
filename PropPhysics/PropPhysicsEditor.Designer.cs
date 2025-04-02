namespace PropPhysics
{
    partial class PropPhysicsEditor
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
            propPhysicsGrid = new DataGridView();
            mainContainer = new SplitContainer();
            okButton = new Button();
            cancelButton = new Button();
            id = new DataGridViewTextBoxColumn();
            jointLocator = new DataGridViewTextBoxColumn();
            comOffset = new DataGridViewTextBoxColumn();
            inertia = new DataGridViewTextBoxColumn();
            resourceId = new DataGridViewTextBoxColumn();
            mass = new DataGridViewTextBoxColumn();
            collisionVolumes = new DataGridViewTextBoxColumn();
            parts = new DataGridViewTextBoxColumn();
            sphereRadius = new DataGridViewTextBoxColumn();
            maxJointAngleCos = new DataGridViewTextBoxColumn();
            leanThreshold = new DataGridViewTextBoxColumn();
            moveThreshold = new DataGridViewTextBoxColumn();
            smashThreshold = new DataGridViewTextBoxColumn();
            sceneUriId = new DataGridViewTextBoxColumn();
            maxState = new DataGridViewTextBoxColumn();
            numberOfParts = new DataGridViewTextBoxColumn();
            numberOfVolumes = new DataGridViewTextBoxColumn();
            jointType = new DataGridViewTextBoxColumn();
            extraTypeInfo = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)propPhysicsGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)mainContainer).BeginInit();
            mainContainer.Panel1.SuspendLayout();
            mainContainer.Panel2.SuspendLayout();
            mainContainer.SuspendLayout();
            SuspendLayout();
            // 
            // propPhysicsGrid
            // 
            propPhysicsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            propPhysicsGrid.Columns.AddRange(new DataGridViewColumn[] { id, jointLocator, comOffset, inertia, resourceId, mass, collisionVolumes, parts, sphereRadius, maxJointAngleCos, leanThreshold, moveThreshold, smashThreshold, sceneUriId, maxState, numberOfParts, numberOfVolumes, jointType, extraTypeInfo });
            propPhysicsGrid.Dock = DockStyle.Fill;
            propPhysicsGrid.Location = new Point(0, 0);
            propPhysicsGrid.Name = "propPhysicsGrid";
            propPhysicsGrid.Size = new Size(800, 407);
            propPhysicsGrid.TabIndex = 0;
            propPhysicsGrid.CellValueChanged += OnCellValueChanged;
            // 
            // mainContainer
            // 
            mainContainer.Dock = DockStyle.Fill;
            mainContainer.FixedPanel = FixedPanel.Panel2;
            mainContainer.Location = new Point(0, 0);
            mainContainer.Name = "mainContainer";
            mainContainer.Orientation = Orientation.Horizontal;
            // 
            // mainContainer.Panel1
            // 
            mainContainer.Panel1.Controls.Add(propPhysicsGrid);
            // 
            // mainContainer.Panel2
            // 
            mainContainer.Panel2.Controls.Add(okButton);
            mainContainer.Panel2.Controls.Add(cancelButton);
            mainContainer.Panel2.Padding = new Padding(0, 0, 10, 0);
            mainContainer.Size = new Size(800, 450);
            mainContainer.SplitterDistance = 407;
            mainContainer.TabIndex = 1;
            // 
            // okButton
            // 
            okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            okButton.Location = new Point(635, 2);
            okButton.Name = "okButton";
            okButton.Size = new Size(75, 32);
            okButton.TabIndex = 0;
            okButton.Text = "OK";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += okButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancelButton.Location = new Point(715, 2);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(75, 32);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // id
            // 
            id.HeaderText = "ID";
            id.Name = "id";
            id.ReadOnly = true;
            // 
            // jointLocator
            // 
            jointLocator.HeaderText = "Joint Locator";
            jointLocator.Name = "jointLocator";
            jointLocator.ReadOnly = true;
            // 
            // comOffset
            // 
            comOffset.HeaderText = "COM Offset";
            comOffset.Name = "comOffset";
            comOffset.ReadOnly = true;
            // 
            // inertia
            // 
            inertia.HeaderText = "Inertia";
            inertia.Name = "inertia";
            inertia.ReadOnly = true;
            // 
            // resourceId
            // 
            resourceId.HeaderText = "Resource ID";
            resourceId.Name = "resourceId";
            // 
            // mass
            // 
            mass.HeaderText = "Mass";
            mass.Name = "mass";
            // 
            // collisionVolumes
            // 
            collisionVolumes.HeaderText = "Collision Volumes";
            collisionVolumes.Name = "collisionVolumes";
            collisionVolumes.ReadOnly = true;
            // 
            // parts
            // 
            parts.HeaderText = "Parts";
            parts.Name = "parts";
            parts.ReadOnly = true;
            // 
            // sphereRadius
            // 
            sphereRadius.HeaderText = "Sphere Radius";
            sphereRadius.Name = "sphereRadius";
            // 
            // maxJointAngleCos
            // 
            maxJointAngleCos.HeaderText = "Max Joint Angle Cos";
            maxJointAngleCos.Name = "maxJointAngleCos";
            // 
            // leanThreshold
            // 
            leanThreshold.HeaderText = "Lean Threshold";
            leanThreshold.Name = "leanThreshold";
            // 
            // moveThreshold
            // 
            moveThreshold.HeaderText = "Move Threshold";
            moveThreshold.Name = "moveThreshold";
            // 
            // smashThreshold
            // 
            smashThreshold.HeaderText = "Smash Threshold";
            smashThreshold.Name = "smashThreshold";
            // 
            // sceneUriId
            // 
            sceneUriId.HeaderText = "Scene URI ID";
            sceneUriId.Name = "sceneUriId";
            // 
            // maxState
            // 
            maxState.HeaderText = "Max State";
            maxState.Name = "maxState";
            // 
            // numberOfParts
            // 
            numberOfParts.HeaderText = "Number of Parts";
            numberOfParts.Name = "numberOfParts";
            numberOfParts.ReadOnly = true;
            // 
            // numberOfVolumes
            // 
            numberOfVolumes.HeaderText = "Number of Volumes";
            numberOfVolumes.Name = "numberOfVolumes";
            numberOfVolumes.ReadOnly = true;
            // 
            // jointType
            // 
            jointType.HeaderText = "Joint Type";
            jointType.Name = "jointType";
            jointType.Resizable = DataGridViewTriState.True;
            // 
            // extraTypeInfo
            // 
            extraTypeInfo.HeaderText = "Extra Type Info";
            extraTypeInfo.Name = "extraTypeInfo";
            extraTypeInfo.Resizable = DataGridViewTriState.True;
            // 
            // PropPhysicsEditor
            // 
            AcceptButton = okButton;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(800, 450);
            Controls.Add(mainContainer);
            Name = "PropPhysicsEditor";
            Text = "PropPhysicsEditor";
            ((System.ComponentModel.ISupportInitialize)propPhysicsGrid).EndInit();
            mainContainer.Panel1.ResumeLayout(false);
            mainContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mainContainer).EndInit();
            mainContainer.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private DataGridView propPhysicsGrid;
        private SplitContainer mainContainer;
        private Button okButton;
        private Button cancelButton;
        private DataGridViewTextBoxColumn id;
        private DataGridViewTextBoxColumn jointLocator;
        private DataGridViewTextBoxColumn comOffset;
        private DataGridViewTextBoxColumn inertia;
        private DataGridViewTextBoxColumn resourceId;
        private DataGridViewTextBoxColumn mass;
        private DataGridViewTextBoxColumn collisionVolumes;
        private DataGridViewTextBoxColumn parts;
        private DataGridViewTextBoxColumn sphereRadius;
        private DataGridViewTextBoxColumn maxJointAngleCos;
        private DataGridViewTextBoxColumn leanThreshold;
        private DataGridViewTextBoxColumn moveThreshold;
        private DataGridViewTextBoxColumn smashThreshold;
        private DataGridViewTextBoxColumn sceneUriId;
        private DataGridViewTextBoxColumn maxState;
        private DataGridViewTextBoxColumn numberOfParts;
        private DataGridViewTextBoxColumn numberOfVolumes;
        private DataGridViewTextBoxColumn jointType;
        private DataGridViewTextBoxColumn extraTypeInfo;
    }
}
