﻿using Entidades;
using OfficeOpenXml;
using ReglasdeNegocio;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;

namespace DBMETAL_SHARP
{
    public partial class Frm_CargaAnalisis : Form
    {
        #region Variables
        public string Usuario { get; set; }
        public string IpLocal { get; set; }
        public string IpPublica { get; set; }
        public string SerialHDD { get; set; }

        public List<Roles_Permisos> Permission;
        public Frm_CargaAnalisis()
        {
            InitializeComponent();
        }
        public Frm_CargaAnalisis(string User)
        {
            InitializeComponent();
            this.Usuario = User.Trim();

            Common.Common.Permissions = ConsultaEntidades.GetPermisosRoles("SPGet_RolesForUser", User.ToString().Trim(), this.Name);

            this.Permission = Common.Common.Permissions;

            ValidatePermission(this.Controls);

        }
        #endregion Variables

        #region Variables
        public int typeFile { get; set; }
        public string valueMedia = string.Empty;
        private string descripcionArchivo = string.Empty;
        public DataSet dataSet = new DataSet();

        #endregion Variables

        #region Metodos
        private void ValidatePermission(Control.ControlCollection controlCollection)
        {
            foreach (Control c in controlCollection)
            {
                if (c.Controls.Count > 0)
                {
                    ValidatePermission(c.Controls);
                }
                if (c is MenuStrip)
                {
                    MenuStrip menuStrip = c as MenuStrip;
                    ShowToolStipItems(menuStrip.Items);
                }

                if (c is Button || c is ComboBox || c is TextBox ||
                    c is ListBox || c is DataGridView || c is System.Windows.Forms.RadioButton ||
                    c is RichTextBox || c is TabPage || c is TextBox || c is GroupBox)
                {

                    Roles_Permisos valueFilter = Permission.Where(e => e.fkcontrolid == c.Name).FirstOrDefault();

                    if (valueFilter != null)
                    {
                        if (valueFilter.Invisible > 0)
                        {
                            c.Visible = false;
                        }
                        else
                        {
                            c.Visible = true;
                        }

                        if (valueFilter.Disabled > 0)
                        {
                            c.Enabled = false;
                        }
                        else
                        {
                            c.Enabled = true;
                        }
                    }
                }
            }
        }

        private void ShowToolStipItems(ToolStripItemCollection toolStripItems)
        {
            foreach (ToolStripMenuItem mi in toolStripItems)
            {
                mi.ToolTipText = mi.Name;

                if (mi.DropDownItems.Count > 0)
                {
                    ShowToolStipItems(mi.DropDownItems);
                }

                Roles_Permisos valueFilter = Permission.Where(e => e.fkcontrolid == mi.Name).FirstOrDefault();

                if (valueFilter != null)
                {
                    if (valueFilter.Invisible > 0)
                    {
                        mi.Visible = false;
                    }
                    else
                    {
                        mi.Visible = true;
                    }

                    if (valueFilter.Disabled > 0)
                    {
                        mi.Enabled = false;
                    }
                    else
                    {
                        mi.Enabled = true;
                    }
                }
            }
        }


        #endregion

        #region Eventos
        private void CmdExaminar_Click(object sender, EventArgs e)
        {
            if (this.Txtruta.Text == string.Empty)
            {
                MessageBox.Show("Debe seleccionar un archivo para cargar.", "Carga de Análisis", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                if (this.CmbTipoIngreso.Text == string.Empty)
                {
                    MessageBox.Show("Debe seleccionar un tipo de ingreso para el carge del archivo de carga.", "Carga de Análisis", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                else
                {
                    if (this.Txtruta.Text != string.Empty)
                    {
                        try
                        {
                            Cargar();
                            if (string.IsNullOrEmpty(this.IpLocal))
                            {
                                this.IpLocal = DireccionIP.Local();
                            }
                            if (string.IsNullOrEmpty(this.IpPublica))
                            {
                                this.IpPublica = DireccionIP.Publica();
                            }
                            if (string.IsNullOrEmpty(this.SerialHDD))
                            {
                                this.SerialHDD = DireccionIP.SerialNumberDisk();
                            }
                            if (string.IsNullOrEmpty(this.Usuario))
                            {
                                this.Usuario = DireccionIP.SerialNumberDisk();
                            }
                            LlenarLog.Registro(DateTime.Now, this.Usuario, this.IpLocal, this.IpPublica, this.SerialHDD, Environment.MachineName, "Carga Analisis Laboratorio, Archivo " + this.descripcionArchivo, "Movimiento Muestreo creado");
                        }
                        catch (OleDbException ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
            }
        }

        internal void Cargar()
        {
            try
            {
                GuardarDatos guardarDatos = new GuardarDatos();

                // Analisis Lab SGS
                if (dataSet.Tables[0].Rows[2][1].ToString().Trim().ToUpper().Contains("PEQUEÑA MINERÍA"))
                {
                    try
                    {
                        LblTitulos.Text = "Análisis Químico Pequeña Minería";
                        typeFile = 0;
                        string value = string.Empty;
                        string idLab = dataSet.Tables[0].Rows[0][1].ToString().Trim();
                        for (int j = 9; j < dataSet.Tables[0].Rows.Count; j++)
                        {
                            string text = dataSet.Tables[0].Rows[j][0].ToString().Trim();
                            if (string.IsNullOrEmpty(value))
                            {
                                value = text;
                            }
                            else
                            {
                                if (!text.Contains(value))
                                {
                                    value = text;
                                }
                            }

                            NumberStyles style = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowCurrencySymbol;
                            CultureInfo provider = CultureInfo.CreateSpecificCulture("ES-CO");

                            try
                            {
                                if (!string.IsNullOrEmpty(text) && !text.ToUpper().Trim().Contains("BLANK_PREP") && !text.ToUpper().Trim().Contains("STD") && !text.ToUpper().Trim().Contains("STD") && !text.ToUpper().Trim().Contains("BLANK")
                                    && !text.ToUpper().Trim().Contains("BLK BLANK"))
                                {
                                    decimal num = 0m;
                                    decimal au = 0m;
                                    decimal ag = 0m;
                                    decimal peso = 0m;
                                    if (!string.IsNullOrEmpty(dataSet.Tables[0].Rows[j][2].ToString().Trim()) && !decimal.TryParse(dataSet.Tables[0].Rows[j][2].ToString().Trim(), style, provider, out num))
                                    {
                                        num = decimal.Parse(dataSet.Tables[0].Rows[j][2].ToString().Trim());
                                    }
                                    if (!string.IsNullOrEmpty(dataSet.Tables[0].Rows[j][3].ToString().Trim()) && !decimal.TryParse(dataSet.Tables[0].Rows[j][3].ToString(), style, provider, out au))
                                    {
                                        au = decimal.Parse(dataSet.Tables[0].Rows[j][3].ToString().Trim());
                                    }
                                    if (!string.IsNullOrEmpty(dataSet.Tables[0].Rows[j][4].ToString().Trim()) && !decimal.TryParse(dataSet.Tables[0].Rows[j][4].ToString().Trim(), style, provider, out ag))
                                    {
                                        ag = decimal.Parse(dataSet.Tables[0].Rows[j][4].ToString().Trim());
                                    }
                                    if (!string.IsNullOrEmpty(dataSet.Tables[0].Rows[j][5].ToString().Trim()) && !decimal.TryParse(dataSet.Tables[0].Rows[j][5].ToString().Trim(), style, provider, out peso))
                                    {
                                        peso = decimal.Parse(dataSet.Tables[0].Rows[j][5].ToString().Trim());
                                    }
                                    SqlParameter[] array = GuardarDatos.Parametros_DetalleExcelPM("", text, num, au, ag, peso, "1", idLab, CmbTipoIngreso.Text);
                                    guardarDatos.Numerico("Sp_Moficiar_AnaQuiPM", array);
                                    if (num > decimal.Zero)
                                    {
                                        array = GuardarDatos.Parametros_ToneladaSeca(text, num);
                                        guardarDatos.Numerico("Sp_Moficiar_ToneladasSeca_MuestreoPM", array);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Sello:" + text + " EX:" + ex.Message, "selloControl");
                                throw;
                            }
                        }
                        MessageBox.Show("Importacion Finalizada", "DB Metal", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    }
                    catch (Exception ex2)
                    {
                        MessageBox.Show(ex2.Message);
                    }
                }

                if (CmbTipoIngreso.Text == "Reclamos")
                {
                    if (dataSet.Tables[0].Columns[0].ToString().Trim().ToUpper().Contains("INFORME DE RECLAMOS"))
                    {
                        LblTitulos.Text = "INFORME DE RECLAMOS";
                        typeFile = 2;
                        for (int k = 44; k < dataSet.Tables[0].Rows.Count; k++)
                        {
                            string selloControl = dataSet.Tables[0].Rows[k][0].ToString().Replace(" ", "");
                            if (string.IsNullOrEmpty(selloControl))
                            {
                                break;
                            }
                            decimal humedad = 0m;
                            if (!string.IsNullOrEmpty(dataSet.Tables[0].Rows[k][3].ToString().Trim()) && !decimal.TryParse(dataSet.Tables[0].Rows[k][3].ToString().Trim(), out humedad))
                            {
                                humedad = decimal.Parse(dataSet.Tables[0].Rows[k][3].ToString().Trim().Substring(1, dataSet.Tables[0].Rows[k][3].ToString().Length - 1));
                            }
                            SqlParameter[] pparametros = GuardarDatos.Parametros_DetalleExcelHumedad("", selloControl, humedad, "1");
                            guardarDatos.Numerico("Sp_Moficiar_AnaQuiHum", pparametros);
                        }
                        MessageBox.Show("Importacion Finalizada");
                    }

                    if (dataSet.Tables[0].Columns[0].ToString().Trim().ToUpper().Contains("ACTLABS"))
                    {
                        LblTitulos.Text = "ACTLABS Colombia S.A.S.";
                        typeFile = 2;

                        string IdLab = dataSet.Tables[0].Rows[1][1].ToString();

                        for (int k = 12; k < dataSet.Tables[0].Rows.Count; k++)
                        {
                            string selloControl = dataSet.Tables[0].Rows[k][0].ToString().Replace(" ", "");
                            if (string.IsNullOrEmpty(selloControl))
                            {
                                break;
                            }

                            decimal au = 0;
                            decimal ag = 0;
                            decimal peso = 0;

                            if (string.IsNullOrEmpty(dataSet.Tables[0].Rows[k][1].ToString().Trim()))
                            {
                                if (string.IsNullOrEmpty(dataSet.Tables[0].Rows[k][2].ToString().Trim()))
                                {
                                    if (string.IsNullOrEmpty(dataSet.Tables[0].Rows[k][3].ToString().Trim()))
                                    {
                                        if (!string.IsNullOrEmpty(dataSet.Tables[0].Rows[k][11].ToString().Trim()))
                                        {
                                            if (!decimal.TryParse(dataSet.Tables[0].Rows[k][11].ToString().Trim(), out au))
                                            {
                                                au = decimal.Parse(dataSet.Tables[0].Rows[k][11].ToString().Trim().Substring(1, dataSet.Tables[0].Rows[k][11].ToString().Length - 1));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!decimal.TryParse(dataSet.Tables[0].Rows[k][3].ToString().Trim(), out au))
                                        {
                                            au = decimal.Parse(dataSet.Tables[0].Rows[k][3].ToString().Trim().Substring(1, dataSet.Tables[0].Rows[k][3].ToString().Length - 1));
                                        }
                                    }
                                }
                                else
                                {
                                    if (!decimal.TryParse(dataSet.Tables[0].Rows[k][2].ToString().Trim(), out au))
                                    {
                                        au = decimal.Parse(dataSet.Tables[0].Rows[k][2].ToString().Trim().Substring(1, dataSet.Tables[0].Rows[k][2].ToString().Length - 1));
                                    }
                                }
                            }
                            else
                            {
                                /* Modificado Alvaro Araujo 06/06/2019 */
                                string valorEntrada = string.Empty;
                                if (dataSet.Tables[0].Rows[k][1].ToString().Contains("<"))
                                {
                                    valorEntrada = dataSet.Tables[0].Rows[k][1].ToString();
                                    var resultante = valorEntrada.Trim(new Char[] { ' ', '<' });
                                    var valorCambio = (Convert.ToDouble(resultante) / 2);
                                    var valoreRedondeo = Math.Round(valorCambio * 1000);
                                    au = Convert.ToDecimal(valoreRedondeo / 1000);
                                }
                                else if (dataSet.Tables[0].Rows[k][1].ToString().Contains(">"))
                                {
                                    valorEntrada = dataSet.Tables[0].Rows[k][1].ToString();
                                    var resultante = valorEntrada.Trim(new Char[] { ' ', '>' });

                                    if (Convert.ToDouble(resultante) == 5.001)
                                    {
                                        if (string.IsNullOrEmpty(dataSet.Tables[0].Rows[k][2].ToString().Trim()))
                                        {
                                            if (string.IsNullOrEmpty(dataSet.Tables[0].Rows[k][3].ToString().Trim()))
                                            {
                                                if (!string.IsNullOrEmpty(dataSet.Tables[0].Rows[k][11].ToString().Trim()))
                                                {
                                                    if (!decimal.TryParse(dataSet.Tables[0].Rows[k][11].ToString().Trim(), out au))
                                                    {
                                                        au = decimal.Parse(dataSet.Tables[0].Rows[k][11].ToString().Trim().Substring(1, dataSet.Tables[0].Rows[k][11].ToString().Length - 1));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (!decimal.TryParse(dataSet.Tables[0].Rows[k][3].ToString().Trim(), out au))
                                                {
                                                    au = decimal.Parse(dataSet.Tables[0].Rows[k][3].ToString().Trim().Substring(1, dataSet.Tables[0].Rows[k][3].ToString().Length - 1));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (!decimal.TryParse(dataSet.Tables[0].Rows[k][2].ToString().Trim(), out au))
                                            {
                                                au = decimal.Parse(dataSet.Tables[0].Rows[k][2].ToString().Trim().Substring(1, dataSet.Tables[0].Rows[k][2].ToString().Length - 1));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var valorCambio = (Convert.ToDouble(resultante) + 0.001);
                                        au = Convert.ToDecimal(valorCambio);
                                    }
                                }
                                else
                                {
                                    if (!decimal.TryParse(dataSet.Tables[0].Rows[k][1].ToString().Trim(), out au))
                                    {
                                        au = decimal.Parse(dataSet.Tables[0].Rows[k][1].ToString().Trim().Substring(1, dataSet.Tables[0].Rows[k][2].ToString().Length - 1));
                                    }
                                }
                            }


                            if (!string.IsNullOrEmpty(dataSet.Tables[0].Rows[k][12].ToString().Trim()) && !decimal.TryParse(dataSet.Tables[0].Rows[k][12].ToString().Trim(), out peso))
                            {
                                peso = decimal.Parse(dataSet.Tables[0].Rows[k][12].ToString().Trim().Substring(1, dataSet.Tables[0].Rows[k][12].ToString().Length - 1));
                            }

                            ag = au;

                            SqlParameter[] pparametros = GuardarDatos.CargaReclamosActLab(selloControl, IdLab, 0, au, ag, peso, CmbTipoIngreso.Text.ToString(), "1");
                            guardarDatos.Numerico("Sp_Moficiar_ReclamosActlabs", pparametros);
                        }
                        MessageBox.Show("Importacion Finalizada");

                        /* ***************************************************************************************  */
                    }
                }
                else
                {
                    if (dataSet.Tables[0].Rows[15][3].ToString().Trim().ToUpper().Contains("HUM"))
                    {
                        LblTitulos.Text = "Humedad Laboratorio Zandor";
                        typeFile = 2;
                        for (int l = 45; l < dataSet.Tables[0].Rows.Count; l++)
                        {
                            string text3 = dataSet.Tables[0].Rows[l][0].ToString().Replace(" ", "");
                            if (string.IsNullOrEmpty(text3))
                            {
                                break;
                            }
                            decimal humedad2 = 0m;
                            if (!string.IsNullOrEmpty(dataSet.Tables[0].Rows[l][3].ToString().Trim()) && !decimal.TryParse(dataSet.Tables[0].Rows[l][3].ToString().Trim(), out humedad2))
                            {
                                humedad2 = decimal.Parse(dataSet.Tables[0].Rows[l][3].ToString().Trim().Substring(1, dataSet.Tables[0].Rows[l][3].ToString().Length - 1));
                            }
                            SqlParameter[] pparametros2 = GuardarDatos.Parametros_DetalleExcelHumedad("", text3, humedad2, "1");
                            guardarDatos.Numerico("Sp_Moficiar_AnaQuiHum", pparametros2);
                        }
                        MessageBox.Show("Importacion Finalizada");
                    }
                    
                    // Renalisis y Retalla
                    if (dataSet.Tables[0].Rows[0][0].ToString().Trim().ToUpper().Contains("LABORATORIO") && dataSet.Tables[0].Rows[1][0].ToString().Trim().ToUpper().Contains("REPORTE DE ANÁLISIS QUÍMICO"))
                    {
                        LblTitulos.Text = "Reporte de Análisis Químico";
                        typeFile = 1;
                        for (int m = 45; m < dataSet.Tables[0].Rows.Count; m++)
                        {
                            string text4 = dataSet.Tables[0].Rows[m][0].ToString().Replace(" ", "");

                            if (!text4.ToUpper().Contains("DUPLIC"))
                            {
                                if (text4.Contains("+") || text4.Contains("-"))
                                {
                                    int length = text4.IndexOf("(");
                                    text4 = text4.Substring(0, length);

                                    if (dataSet.Tables[0].Rows[m][0].ToString().Contains("-"))
                                    {
                                        text4 += "A";
                                    }

                                    if (string.IsNullOrEmpty(text4))
                                    {
                                        break;
                                    }
                                        
                                }

                                decimal au2 = 0m;
                                decimal augr = 0m;
                                decimal peso = 0;
                                if (!string.IsNullOrEmpty(dataSet.Tables[0].Rows[m][1].ToString().Trim()) && !decimal.TryParse(dataSet.Tables[0].Rows[m][1].ToString().Trim(), out au2))
                                {
                                    au2 = decimal.Parse(dataSet.Tables[0].Rows[m][1].ToString().Trim().Substring(1, dataSet.Tables[0].Rows[m][1].ToString().Length - 1));
                                }

                                if (!string.IsNullOrEmpty(dataSet.Tables[0].Rows[m][2].ToString().Trim()) && !decimal.TryParse(dataSet.Tables[0].Rows[m][2].ToString().Trim(), out augr))
                                {
                                    augr = decimal.Parse(dataSet.Tables[0].Rows[m][2].ToString().Trim().Substring(1, dataSet.Tables[0].Rows[m][2].ToString().Length - 1));
                                }

                                if (!string.IsNullOrEmpty(dataSet.Tables[0].Rows[m][2].ToString().Trim()) && !decimal.TryParse(dataSet.Tables[0].Rows[m][2].ToString().Trim(), out peso))
                                {
                                    peso = decimal.Parse(dataSet.Tables[0].Rows[m][2].ToString().Trim().Substring(0, dataSet.Tables[0].Rows[m][2].ToString().Length));
                                }

                                SqlParameter[] pparametros3 = GuardarDatos.Parametros_DetalleExcelZandor("", text4, au2, augr, peso, "1", CmbTipoIngreso.Text.ToString());
                                guardarDatos.Numerico("Sp_Moficiar_AnaQuiZandor", pparametros3);
                            }
                        }
                        MessageBox.Show("Importacion Finalizada", "DB Metal", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }                                         
                }
            }
            catch (Exception ex3)
            {
                MessageBox.Show(ex3.Message, "Mensaje de Execpción", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void CargaExcelCompleto(DataGridView dgView, string SLibro)
        {
            OleDbConnection oleDbConnection;
            try
            {
                oleDbConnection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.16.0;Data Source=" + SLibro + ";Extended Properties=\"Excel 16.0;HDR=NO;IMEX=1\"");
                oleDbConnection.Open();
            }
            catch (Exception)
            {
                try
                {
                    oleDbConnection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + SLibro + ";Extended Properties=\"Excel 8.0;HDR=NO;IMEX=1\"");
                    oleDbConnection.Open();
                }
                catch (Exception)
                {
                    oleDbConnection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.15.0;Data Source=" + SLibro + ";Extended Properties=\"Excel 8.0;HDR=NO;IMEX=1\"");
                    oleDbConnection.Open();
                }
            }

            DataTable oleDbSchemaTable = oleDbConnection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            string str = string.Empty;
            if (oleDbSchemaTable != null)
            {
                for (int i = 0; i < oleDbSchemaTable.Rows.Count; i++)
                {
                    if (!oleDbSchemaTable.Rows[i]["TABLE_NAME"].ToString().ToUpper().Trim().Contains("PRINT_TITLES"))
                    {
                        str = oleDbSchemaTable.Rows[i]["TABLE_NAME"].ToString().Trim();
                        break;
                    }
                }
            }
            OleDbDataAdapter oleDbDataAdapter = new OleDbDataAdapter("select * from [" + str + "]", oleDbConnection);
            oleDbDataAdapter.TableMappings.Add("Table", "TestTable");

            oleDbDataAdapter.Fill(dataSet);
            dgView.DataSource = dataSet.Tables[0];
            dgView.AutoResizeColumns();
            oleDbConnection.Close();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            ActiveForm.Close();
            Dispose(true);
        }
        #endregion Eventos

        #region Metodos
        public DataTable getDataTableFromExcel(string path)
        {
            DataTable result;
            using (ExcelPackage excelPackage = new ExcelPackage())
            {
                using (FileStream fileStream = File.OpenRead(path))
                {
                    excelPackage.Load(fileStream);
                }
                ExcelWorksheet excelWorksheet = excelPackage.Workbook.Worksheets.First<ExcelWorksheet>();
                DataTable dataTable = new DataTable();
                bool flag = true;
                foreach (ExcelRangeBase current in excelWorksheet.Cells[1, 1, 1, excelWorksheet.Dimension.End.Column])
                {
                    dataTable.Columns.Add(flag ? current.Text : string.Format("Column {0}", current.Start.Column));
                }
                int num = flag ? 2 : 1;
                for (int i = num; i <= excelWorksheet.Dimension.End.Row; i++)
                {
                    ExcelRange excelRange = excelWorksheet.Cells[i, 1, i, excelWorksheet.Dimension.End.Column];
                    DataRow dataRow = dataTable.NewRow();
                    foreach (ExcelRangeBase current2 in excelRange)
                    {
                        dataRow[current2.Start.Column - 1] = current2.Text;
                    }
                    dataTable.Rows.Add(dataRow);
                }
                result = dataTable;
            }
            return result;
        }
        public static object GetMedian<T>(T[] sourceArray, bool cloneArray = true) where T : IComparable<T>
        {
            //Framework 2.0 version of this method. there is an easier way in F4        
            if (sourceArray == null || sourceArray.Length == 0)
            {
                throw new ArgumentException("Median of empty array not defined.");
            }

            //make sure the list is sorted, but use a new array
            T[] sortedArray = cloneArray ? (T[])sourceArray.Clone() : sortedArray = sourceArray;
            Array.Sort(sortedArray);

            //get the median
            int size = sortedArray.Length;
            int mid = size / 2;
            if (size % 2 != 0)
            {
                return sortedArray[mid];
            }

            dynamic value1 = sortedArray[mid];
            dynamic value2 = sortedArray[mid - 1];
            var side = (double)Convert.ToDouble(sortedArray[mid] + value1) * 0.5;
            return side;

            //return (sortedArray[mid] + value2) * 0.5;
        }

        private decimal Median(decimal[] xs)
        {
            var ys = xs.OrderBy(x => x).ToList();
            double mid = (ys.Count - 1) / 2.0;
            return (ys[(int)(mid)] + ys[(int)(mid + 0.5)]) / 2;
        }
        #endregion Metodos

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
            Dispose(true);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            dtgExcel.DataSource = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Seleccionar archivos";
            openFileDialog.Filter = "Todos los archivos  (*.xls) | *.xlsx;*.xls";
            openFileDialog.Multiselect = false;
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
            this.descripcionArchivo = openFileDialog.FileName;

            //if (openFileDialog.ShowDialog() == DialogResult.OK)
            //    Txtruta.Text = openFileDialog.FileName;

            //if (Txtruta.Text != string.Empty)
            //    CargaExcelCompleto(dtgExcel, Txtruta.Text);

            CargaExcelFormaSinProvider(openFileDialog);
        }


        private void CargaExcelFormaSinProvider(OpenFileDialog openFileDialog)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                label2.Visible = true;
                Txtruta.Text = openFileDialog.FileName;
                FileInfo oFi = new FileInfo(openFileDialog.FileName);
                string sExt = oFi.Extension.ToString();
                descripcionArchivo = oFi.Name.Substring(0, oFi.Name.ToString().Length - sExt.ToString().Length);

                Excel.Application oExc = new Excel.Application();
                oExc.Workbooks.Open(Txtruta.Text, 0, true, 5, Type.Missing, Type.Missing, false, Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, null, null);
                oExc.Quit();
            }
            try
            {
                DataTable dtXls = new DataTable();
                Workbook workbook = new Workbook();

                workbook.LoadFromFile(Txtruta.Text);
                Worksheet sheet = workbook.Worksheets[0];
                dtXls = sheet.ExportDataTable();
                dataSet.Tables.Add(dtXls);
                dtgExcel.DataSource = dtXls;
                dtgExcel.AutoResizeColumns();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
