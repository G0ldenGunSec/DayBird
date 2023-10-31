
namespace DayBird
{
    partial class AutoRunForm
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
            this.components = new System.ComponentModel.Container();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Priority = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.PluginName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Enabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.RequiresConfig = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.assemblyNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lastModifiedDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.assemblySizeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.assemblyObjDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.requiresConfigDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.requiredArgsDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.assemblyInstanceBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.assemblyInstanceBindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.assemblyInstanceBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.assemblyInstanceBindingSource1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.AutoGenerateColumns = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Priority,
            this.PluginName,
            this.Enabled,
            this.RequiresConfig,
            this.assemblyNameDataGridViewTextBoxColumn,
            this.lastModifiedDataGridViewTextBoxColumn,
            this.assemblySizeDataGridViewTextBoxColumn,
            this.assemblyObjDataGridViewTextBoxColumn,
            this.requiresConfigDataGridViewTextBoxColumn,
            this.requiredArgsDataGridViewTextBoxColumn});
            this.dataGridView1.DataSource = this.assemblyInstanceBindingSource;
            this.dataGridView1.Location = new System.Drawing.Point(12, 12);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.Size = new System.Drawing.Size(542, 158);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            // 
            // Priority
            // 
            this.Priority.HeaderText = "Priority";
            this.Priority.MaxDropDownItems = 50;
            this.Priority.Name = "Priority";
            this.Priority.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // PluginName
            // 
            this.PluginName.DataPropertyName = "AssemblyName";
            this.PluginName.HeaderText = "Plugin Name";
            this.PluginName.Name = "PluginName";
            this.PluginName.ReadOnly = true;
            // 
            // Enabled
            // 
            this.Enabled.DataPropertyName = "dgvEnabled";
            this.Enabled.HeaderText = "Enabled";
            this.Enabled.Name = "Enabled";
            // 
            // RequiresConfig
            // 
            this.RequiresConfig.DataPropertyName = "RequiresConfig";
            this.RequiresConfig.HeaderText = "Requires Config";
            this.RequiresConfig.Name = "RequiresConfig";
            this.RequiresConfig.ReadOnly = true;
            this.RequiresConfig.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.RequiresConfig.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // assemblyNameDataGridViewTextBoxColumn
            // 
            this.assemblyNameDataGridViewTextBoxColumn.DataPropertyName = "AssemblyName";
            this.assemblyNameDataGridViewTextBoxColumn.HeaderText = "AssemblyName";
            this.assemblyNameDataGridViewTextBoxColumn.Name = "assemblyNameDataGridViewTextBoxColumn";
            this.assemblyNameDataGridViewTextBoxColumn.Visible = false;
            // 
            // lastModifiedDataGridViewTextBoxColumn
            // 
            this.lastModifiedDataGridViewTextBoxColumn.DataPropertyName = "LastModified";
            this.lastModifiedDataGridViewTextBoxColumn.HeaderText = "LastModified";
            this.lastModifiedDataGridViewTextBoxColumn.Name = "lastModifiedDataGridViewTextBoxColumn";
            this.lastModifiedDataGridViewTextBoxColumn.Visible = false;
            // 
            // assemblySizeDataGridViewTextBoxColumn
            // 
            this.assemblySizeDataGridViewTextBoxColumn.DataPropertyName = "AssemblySize";
            this.assemblySizeDataGridViewTextBoxColumn.HeaderText = "AssemblySize";
            this.assemblySizeDataGridViewTextBoxColumn.Name = "assemblySizeDataGridViewTextBoxColumn";
            this.assemblySizeDataGridViewTextBoxColumn.Visible = false;
            // 
            // assemblyObjDataGridViewTextBoxColumn
            // 
            this.assemblyObjDataGridViewTextBoxColumn.DataPropertyName = "AssemblyObj";
            this.assemblyObjDataGridViewTextBoxColumn.HeaderText = "AssemblyObj";
            this.assemblyObjDataGridViewTextBoxColumn.Name = "assemblyObjDataGridViewTextBoxColumn";
            this.assemblyObjDataGridViewTextBoxColumn.Visible = false;
            // 
            // requiresConfigDataGridViewTextBoxColumn
            // 
            this.requiresConfigDataGridViewTextBoxColumn.DataPropertyName = "RequiresConfig";
            this.requiresConfigDataGridViewTextBoxColumn.HeaderText = "RequiresConfig";
            this.requiresConfigDataGridViewTextBoxColumn.Name = "requiresConfigDataGridViewTextBoxColumn";
            this.requiresConfigDataGridViewTextBoxColumn.Visible = false;
            // 
            // requiredArgsDataGridViewTextBoxColumn
            // 
            this.requiredArgsDataGridViewTextBoxColumn.DataPropertyName = "RequiredArgs";
            this.requiredArgsDataGridViewTextBoxColumn.HeaderText = "RequiredArgs";
            this.requiredArgsDataGridViewTextBoxColumn.Name = "requiredArgsDataGridViewTextBoxColumn";
            this.requiredArgsDataGridViewTextBoxColumn.Visible = false;
            // 
            // assemblyInstanceBindingSource
            // 
            this.assemblyInstanceBindingSource.DataSource = typeof(DayBird.AssemblyInstance);
            this.assemblyInstanceBindingSource.CurrentChanged += new System.EventHandler(this.assemblyInstanceBindingSource_CurrentChanged);
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(431, 176);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(123, 24);
            this.button1.TabIndex = 1;
            this.button1.Text = "Continue";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // assemblyInstanceBindingSource1
            // 
            this.assemblyInstanceBindingSource1.DataSource = typeof(DayBird.AssemblyInstance);
            // 
            // AutoRunForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(566, 207);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.dataGridView1);
            this.Name = "AutoRunForm";
            this.Text = "AutoRun Plugins";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.assemblyInstanceBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.assemblyInstanceBindingSource1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.BindingSource assemblyInstanceBindingSource;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.BindingSource assemblyInstanceBindingSource1;
        private System.Windows.Forms.DataGridViewComboBoxColumn Priority;
        private System.Windows.Forms.DataGridViewTextBoxColumn PluginName;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Enabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn RequiresConfig;
        private System.Windows.Forms.DataGridViewTextBoxColumn assemblyNameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn lastModifiedDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn assemblySizeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn assemblyObjDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn requiresConfigDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn requiredArgsDataGridViewTextBoxColumn;
    }
}