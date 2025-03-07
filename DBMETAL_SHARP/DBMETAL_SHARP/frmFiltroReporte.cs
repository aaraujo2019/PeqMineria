﻿using Entidades;
using ReglasdeNegocio;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBMETAL_SHARP
{
    public partial class frmFiltroReporte : Form
    {
        #region Propiedades - Variables

        public string Usuario { get; set; }
        public string IpLocal { get; set; }
        public string IpPublica { get; set; }
        public string SerialHDD { get; set; }

        #endregion

        public frmFiltroReporte()
        {
            InitializeComponent();
            CargarComboBox();
        }

        private void CargarComboBox()
        {
            List<Ent_LiquidacionPeriodos> periodos;
            DataSet proyectos;

            periodos = ConsultaEntidades.ObtenerPeriodosSp();
            cmbPeriodo.DataSource = periodos.OrderBy(p => p.MesPeriodo).ToList();
            cmbPeriodo.ValueMember = "IdPeriodo";
            cmbPeriodo.DisplayMember = "IdPeriodo";
            cmbPeriodo.SelectedIndex = 0;

            proyectos = ConsultaEntidades.CargarProyectos();
            cmbProyecto.DataSource = proyectos.Tables[0];
            cmbProyecto.ValueMember = "NombreProyecto";
            cmbProyecto.DisplayMember = "NombreProyecto";
            cmbProyecto.SelectedIndex = 0;
        }

        private void BtnImprimir_Click(object sender, EventArgs e)
        {
            string wProyecto = string.Empty;
            string wPeriodo = string.Empty;
            if (cmbProyecto.SelectedIndex != 0)
                wProyecto = cmbProyecto.Text;
            
            if (cmbPeriodo.SelectedIndex != 0)
                wPeriodo = cmbPeriodo.Text;

            FrmRptDiarioMuestreo frmReporte = new FrmRptDiarioMuestreo(Convert.ToDateTime(dtpEventInitial.Text).ToString("yyyy-MM-dd"), 
                                                                        Convert.ToDateTime(dtpEventEnd.Text).ToString("yyyy-MM-dd"), wProyecto, wPeriodo, txtNumOrden.Text);
            frmReporte.Show();
            string nameReport = "Reporte Diario Muestreo planta Maria Dama";


            if (string.IsNullOrEmpty(this.IpLocal))
                this.IpLocal = DireccionIP.Local();

            if (string.IsNullOrEmpty(this.IpPublica))
                this.IpPublica = DireccionIP.Publica();

            if (string.IsNullOrEmpty(this.SerialHDD))
                this.SerialHDD = DireccionIP.SerialNumberDisk();

            if (string.IsNullOrEmpty(this.Usuario))
                this.Usuario = DireccionIP.SerialNumberDisk();

            LlenarLog.Registro(DateTime.Now, this.Usuario, this.IpLocal, this.IpPublica, this.SerialHDD, Environment.MachineName, "Se genera el reporte de " + nameReport, "Generación de Reportes");

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
            this.Dispose(true);
        }

        private void frmFiltroReporte_Load(object sender, EventArgs e)
        {
            dtpEventEnd.Value = DateTime.Now;
        }
    }
}
