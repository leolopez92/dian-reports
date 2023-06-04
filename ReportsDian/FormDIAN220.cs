using Microsoft.Reporting.WinForms;
using System;
using System.Data;
using System.IO;
using System.Net.Mail;
using System.Windows.Forms;

namespace DianReportsApp
{
    public partial class FormDIAN220 : Form
    {
        public FormDIAN220()
        {
            InitializeComponent();
        }

        private void FormDIAN220_Load(object sender, EventArgs e)
        {

        }

        private bool ValidatePath(string path)
        {
            if (Directory.Exists(path))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void SavePDF(ReportViewer viewer, string savePath)
        {
            string deviceInfo = "";
            byte[] bytes = viewer.LocalReport.Render("PDF", deviceInfo, out string mimeType, out string encoding, out string extension, out string[] streamIds, out Warning[] warnings);
            using (FileStream stream = new FileStream(@savePath, FileMode.Create))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        private void GenerateDIANReport(string savePath, string cedula)
        {
            ReportViewer viewer = new ReportViewer
            {
                ProcessingMode = ProcessingMode.Local
            };
            viewer.LocalReport.ReportPath = "DIANCertificado220_2022.rdlc";
            try
            {
                if (checkBoxGenerarTodos.Checked)
                {
                    createReport(savePath, 0, viewer);                    
                }
                else
                {
                    if (!string.IsNullOrEmpty(cedula) && int.Parse(cedula) > 0)
                    {
                        createReport(savePath, decimal.Parse(cedula), viewer);                       
                    }
                    else
                    {
                        richTextBoxResultados.Text = "Digite un número de cédula valido";
                    }                    
                }
            }
            catch (Exception ex)
            {                
                richTextBoxResultados.Text = "Error en el proceso: " + ex;
                return;
            }        
        }

        private int SendMail(string savePath, string email)
        {
            string remitente = textBoxRemitente.Text.Trim();
            MailMessage mailMessage = new MailMessage(remitente, email)
            {
                Subject = textBoxAsunto.Text,
                SubjectEncoding = System.Text.Encoding.UTF8,
                Body = richTextBoxCuerpo.Text,
                BodyEncoding = System.Text.Encoding.UTF8,
                IsBodyHtml = true
            };

            Attachment attachment = new Attachment(@savePath);
            string[] subs = savePath.Split('/');
            attachment.Name = subs[subs.Length - 1];
            mailMessage.Attachments.Add(attachment);

            SmtpClient cliente = new SmtpClient
            {
                Credentials = new System.Net.NetworkCredential(remitente, textBoxContrasena.Text.Trim()),
                EnableSsl = true,
                Port = int.Parse(textBoxPuerto.Text.Trim()),
                Host = textBoxHost.Text.Trim()
            };

            using (var client = cliente)
            {
                try
                {
                    cliente.Send(mailMessage);
                    richTextBoxResultados.Text = "Correo enviado con éxito";
                    Console.WriteLine("Correo enviado con éxito");
                    return 1;
                }
                catch (Exception ex)
                {
                    richTextBoxResultados.Text = ("Error al enviar correo: " + ex.Message);
                    Console.WriteLine("Error al enviar correo: " + ex.Message);
                    return 0;
                }
            }
        }

        private void Button1_Click_1(object sender, EventArgs e)
        {
            labelResGenerados.Text = "0";
            labelResEnviados.Text = "0";
            string cedulaString = textBoxCedula.Text.Replace(".", "").Replace(",", "").Replace(" ", "");
            if ((!checkBoxGenerarTodos.Checked && string.IsNullOrEmpty(cedulaString))) /*||
                !Int32.TryParse(cedulaString, out _))*/
            {
                richTextBoxResultados.Text = "Digite un número de cédula valido";
            }
            else if (String.IsNullOrEmpty(textBoxRuta.Text))
            {
                richTextBoxResultados.Text = "Digite una ruta valida ...";
            }
            else
            {
                if (ValidatePath(textBoxRuta.Text))
                {
                    if (string.IsNullOrEmpty(textBoxRemitente.Text) || string.IsNullOrEmpty(textBoxContrasena.Text) ||
                        string.IsNullOrEmpty(textBoxPuerto.Text) || string.IsNullOrEmpty(textBoxHost.Text))
                    {
                        richTextBoxResultados.Text = "Debe llenar todos los campos de la sección Correo";
                    }
                    else
                    {
                        GenerateDIANReport(textBoxRuta.Text, cedulaString);
                    }
                }
                else
                {
                    richTextBoxResultados.Text = "La ruta proporcionada es invalida o no existe";
                }
            }
        }

        public void checkBoxGenerarTodos_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxEnviarCorreo_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBoxCedula_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxCambiarRuta_CheckedChanged(object sender, EventArgs e)
        {
            textBoxRuta.ReadOnly = checkBoxCambiarRuta.Checked;
        }

        private void richTextBoxCuerpo_TextChanged(object sender, EventArgs e)
        {

        }

        private void createReport(string savePath, decimal cedula, ReportViewer viewer)
        {
            int generados = 0;
            int enviados = 0;
            string idRetenedor;
            string razonRetenedor;
            string idTrabajador;
            string email;
            string basePath = savePath;
            this.sp_consultarPersona_2022TableAdapter.Fill(this.adamDataSet.sp_consultarPersona_2022, cedula);            
            if (this.adamDataSet.sp_consultarPersona_2022.Count > 0)
            {
                var dt = this.adamDataSet.sp_consultarPersona_2022;                
                foreach (DataRow dr in dt.Rows)
                {
                    idRetenedor = dr["Id_Retenedor_05"].ToString();
                    razonRetenedor = dr["Razon_Retenedor_11"].ToString();
                    idTrabajador = dr["Id_Trabajador_25"].ToString();
                    email = dr["Email"].ToString();
                    viewer.LocalReport.DataSources.Add(new ReportDataSource("FillReport", this.sp_consultarPersonaBindingSource));
                    savePath = basePath + "/" + razonRetenedor;
                    if (!Directory.Exists(savePath))
                    {
                        Directory.CreateDirectory(savePath);
                        Console.WriteLine(savePath);
                    }
                    SavePDF(viewer, savePath + "/" + (idRetenedor + " - " + idTrabajador + ".pdf"));
                    generados++;
                    enviados = checkBoxEnviarCorreo.Checked ? enviados + SendMail(savePath + "/" + (idRetenedor + " - " + idTrabajador + ".pdf"), email) : 0;
                    viewer.LocalReport.DataSources.Clear();
                    this.sp_consultarPersonaBindingSource.RemoveCurrent();
                }
                richTextBoxResultados.Text = (checkBoxEnviarCorreo.Checked) && (generados != enviados)
                                                    ? "Proceso ejecutado con errores"
                                                        : "Proceso ejecutado con éxito";
            }
            else
            {
                richTextBoxResultados.Text = "No se encontró el número de cédula ingresado";
            }
            labelResGenerados.Text = generados.ToString();
            labelResEnviados.Text = enviados.ToString();
        }

        private void spconsultarPersonaBindingSource_CurrentChanged(object sender, EventArgs e)
        {

        }

        private void bindingSource1_CurrentChanged(object sender, EventArgs e)
        {

        }

        private void richTextBoxResultados_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxAsunto_TextChanged(object sender, EventArgs e)
        {

        }
    }

}