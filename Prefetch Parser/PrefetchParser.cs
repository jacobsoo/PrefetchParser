using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;

namespace Prefetch_Parser
{
    public partial class PrefetchParser : Form
    {
        public PrefetchParser()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // The user wants to exit the application. Close everything down.
            ClearScreen();
            Application.Exit();
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Prefetch Parser by Jacob Soo", "About");
        }

        private void ClearScreen()
        {
            StatusLabel.Text = "";
            StatusLabel.Visible = false;
            exportToToolStripMenuItem.Visible = false;
            cSVToolStripMenuItem.Visible = false;
            xMLToolStripMenuItem.Visible = false;
            dataGridView.DataSource = null;
            dataGridView.Enabled = false;
        }

        private void selectedFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                
                string[] files = Directory.GetFiles(folderBrowserDialog.SelectedPath);
                this.parsepf(files);
            }
        }

        private void parsepf(string[] j)
        {
            ClearScreen();
            tabControl1.TabPages[0].Text = "File Accessed";
            tabControl1.TabPages[1].Text = "Volume Information";
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            DataTable dtPrefetchInfo = new DataTable("pfinfo");
            DataTable dtPrefetchSubInfo = new DataTable("pfinfosub");
            DataTable dtPrefetchVolInfo = new DataTable("pvolfosub");
            dtPrefetchInfo.Columns.Add("FileName", typeof(string));
            dtPrefetchInfo.Columns.Add("Boot Process Name", typeof(string));
            dtPrefetchInfo.Columns.Add("Hash Value", typeof(string));
            dtPrefetchInfo.Columns.Add("Last Run Time (UTC)", typeof(DateTime));
            dtPrefetchInfo.Columns.Add("2nd Last Run Time (UTC)", typeof(DateTime));
            dtPrefetchInfo.Columns.Add("3rd Last Run Time (UTC)", typeof(DateTime));
            dtPrefetchInfo.Columns.Add("4th Last Run Time (UTC)", typeof(DateTime));
            dtPrefetchInfo.Columns.Add("5th Last Run Time (UTC)", typeof(DateTime));
            dtPrefetchInfo.Columns.Add("6th Last Run Time (UTC)", typeof(DateTime));
            dtPrefetchInfo.Columns.Add("7th Last Run Time (UTC)", typeof(DateTime));
            dtPrefetchInfo.Columns.Add("8th Last Run Time (UTC)", typeof(DateTime));
            dtPrefetchInfo.Columns.Add("Run Count", typeof(int));
            dtPrefetchInfo.Columns.Add("Volume Serial", typeof(string));
            dtPrefetchInfo.Columns.Add("Volume Created Date (UTC)", typeof(DateTime));
            dtPrefetchInfo.Columns.Add("MD5 Hash", typeof(string));
            dtPrefetchSubInfo.Columns.Add("FileName", typeof(string));
            dtPrefetchSubInfo.Columns.Add("Path", typeof(string));
            dtPrefetchVolInfo.Columns.Add("FileName", typeof(string));
            dtPrefetchVolInfo.Columns.Add("Path", typeof(string));
            PrefetchProcessingClass pfProcessing = new PrefetchProcessingClass();
            PrefetchInfoClass pf = new PrefetchInfoClass();
            bool isVista = false, isWin8 = false;
            foreach (string filepath in j)
            {
                if (pfProcessing.IsPrefetchFile(filepath))
                {
                    try
                    {
                        if (pfProcessing.CheckHeader(filepath, ref isVista, ref isWin8))
                        {
                            pf.IsVista = isVista;
                            pf.IsWin8 = isWin8;
                            pf.FilePath = filepath;
                            pfProcessing.ParsePfFile(filepath, ref pf);
                            string mD5HashFromFile = GetMD5HashFromFile(pf.FilePath);
                            DateTime? myTime = null;
                            if (!isWin8)
                            {
                                dtPrefetchInfo.Rows.Add(new object[]{
                                    Path.GetFileName(pf.FilePath),
                                    pf.NameWithoutHash,
                                    pf.PathHash,
                                    pf.LastRun,
                                    myTime,
                                    myTime,
                                    myTime,
                                    myTime,
                                    myTime,
                                    myTime,
                                    myTime,
                                    pf.NumTimesExecuted,
                                    pf.VolumeInfo[0].Serial,
                                    pf.VolumeInfo[0].CreatedDate,
								    mD5HashFromFile
							    });
                            }
                            else
                            {
                                dtPrefetchInfo.Rows.Add(new object[]{
                                    Path.GetFileName(pf.FilePath),
                                    pf.NameWithoutHash,
                                    pf.PathHash,
                                    pf.LastRun,
                                    pf.LastRun2,
                                    pf.LastRun3,
                                    pf.LastRun4,
                                    pf.LastRun5,
                                    pf.LastRun6,
                                    pf.LastRun7,
                                    pf.LastRun8,
                                    pf.NumTimesExecuted,
                                    pf.VolumeInfo[0].Serial,
                                    pf.VolumeInfo[0].CreatedDate,
								    mD5HashFromFile
							    });
                            }
                            int iVolInfoCount = pf.VolumeInfo.Count();
                            for (int k = 0; k < iVolInfoCount; k++)
                            {
                                if (pf.VolumeInfo[k] != null)
                                {
                                    int iFolderCount = pf.VolumeInfo[k].FolderPaths.Count();
                                    for (int m = 0; m < iFolderCount; m++)
                                    {
                                        dtPrefetchVolInfo.Rows.Add(new object[]
                            {
                                Path.GetFileName(pf.FilePath),
                                pf.VolumeInfo[k].FolderPaths[m]
                            });
                                    }
                                }
                            }
                            int iFileInfoCount = pf.FilesAccessed.Count();
                            for (int f = 0; f < iFileInfoCount; f++)
                            {
                                dtPrefetchSubInfo.Rows.Add(new object[]
                                {
                                    Path.GetFileName(pf.FilePath),
                                    pf.FilesAccessed[f]
                                });
                            }

                            dataGridView.DataSource = dtPrefetchInfo;
                            dataGridView.Visible = true;
                            dataGridView.Enabled = true;
                            dataGridpfFile.DataSource = dtPrefetchSubInfo;
                            dataGridpfFile.Visible = true;
                            dataGridpfFile.Enabled = true;
                            dataGridpfVolume.DataSource = dtPrefetchVolInfo;
                            dataGridpfVolume.Visible = true;
                            dataGridpfVolume.Enabled = true;
                            exportToToolStripMenuItem.Visible = true;
                            xMLToolStripMenuItem.Visible = true;
                            cSVToolStripMenuItem.Visible = true;
                            dataGridView.Columns["Last Run Time (UTC)"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
                            dataGridView.Columns["Volume Created Date (UTC)"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
                            if (!isWin8)
                            {
                                dataGridView.Columns["2nd Last Run Time (UTC)"].Visible = false;
                                dataGridView.Columns["3rd Last Run Time (UTC)"].Visible = false;
                                dataGridView.Columns["4th Last Run Time (UTC)"].Visible = false;
                                dataGridView.Columns["5th Last Run Time (UTC)"].Visible = false;
                                dataGridView.Columns["6th Last Run Time (UTC)"].Visible = false;
                                dataGridView.Columns["7th Last Run Time (UTC)"].Visible = false;
                                dataGridView.Columns["8th Last Run Time (UTC)"].Visible = false;
                            }
                            stopwatch.Stop();
                            // TSSLabel.Text =  iNum + " files parsed.";
                            string str = stopwatch.Elapsed.TotalSeconds.ToString();
                            TSSLabel2.Text = "Take taken: " + str + " seconds.";
                        }
                    }
                    finally
                    {
                    }
                }
            }
        }

        public string GetMD5HashFromFile(string fileName)
        {
            FileStream inputStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            MD5 mD = new MD5CryptoServiceProvider();
            byte[] toconvert = mD.ComputeHash(inputStream);
            return bytestostring(toconvert);
        }

        public string bytestostring(byte[] toconvert)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < toconvert.Length; i++)
            {
                stringBuilder.Append(toconvert[i].ToString("X2"));
            }
            return stringBuilder.ToString();
        }

        private void singlePrefetchFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PF files (*.pf)|*.pf";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                this.parsepf(new string[] { openFileDialog.FileName });
            }
        }

        private void prefetchFolderInLocalMachineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            object value = Registry.GetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnablePrefetcher", 0);
            value.ToString();
            string path = Environment.GetEnvironmentVariable("SystemRoot") + "\\Prefetch";
            bool flag = CheckAdmin();
            if (!flag)
            {
                MessageBox.Show("The current user is not an administrator, you can't access the folder to the LocalMachine.");
            }
            else
            {
                MessageBox.Show("The current user is an administrator. Processing now.");
                string[] files = Directory.GetFiles(path);
                this.parsepf(files);
            }
        }

        private bool CheckAdmin()
        {
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void cSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CSV_Format(dataGridView);
        }

        private void CSV_Format(DataGridView x)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string value = "\r\n";
                TextWriter textWriter = new StreamWriter(saveFileDialog.FileName);
                DataTable dataTable = x.DataSource as DataTable;
                foreach (DataColumn dataColumn in dataTable.Columns)
                {
                    string value2 = dataColumn.ColumnName + ",";
                    textWriter.Write(value2);
                }
                textWriter.Write(value);
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        string value3 = dataRow[column] + ",";
                        textWriter.Write(value3);
                    }
                    textWriter.Write(value);
                }
                textWriter.Close();
                MessageBox.Show(Path.GetFileName(saveFileDialog.FileName) + " saved sucessfully");
            }
        }

        private void xMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "XML Files (*.XML)|*.XML";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                DataTable dataTable = dataGridView.DataSource as DataTable;
                TextWriter textWriter = new StreamWriter(saveFileDialog.FileName);
                dataTable.WriteXml(textWriter);
                textWriter.Close();
                MessageBox.Show(Path.GetFileName(saveFileDialog.FileName) + " saved sucessfully");
            }
        }

        private void dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            string str = "", rowFilter = "";
            str = this.dataGridView["FileName", e.RowIndex].Value.ToString();
            rowFilter = "FileName = " + "'" + str + "'";
            if (dataGridpfFile.DataSource != null){
                ((DataTable)this.dataGridpfFile.DataSource).DefaultView.RowFilter = rowFilter;
                ((DataTable)this.dataGridpfVolume.DataSource).DefaultView.RowFilter = rowFilter;
            }
        }
    }
}
