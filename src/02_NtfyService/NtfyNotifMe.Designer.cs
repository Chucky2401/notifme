namespace NtfyService {
    partial class NtfyNotifMe {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.serviceController1 = new System.ServiceProcess.ServiceController();
            this.eventLog = new System.Diagnostics.EventLog();
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).BeginInit();
            // 
            // serviceController1
            // 
            this.serviceController1.ServiceName = "NtfyNotifMe";
            // 
            // eventLog
            // 
            this.eventLog.EntryWritten += new System.Diagnostics.EntryWrittenEventHandler(this.EventLog1_EntryWritten);
            // 
            // NtfyNotifMe
            // 
            this.ServiceName = "Service1";
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).EndInit();

        }

        #endregion

        private System.ServiceProcess.ServiceController serviceController1;
        private System.Diagnostics.EventLog eventLog;
    }
}
