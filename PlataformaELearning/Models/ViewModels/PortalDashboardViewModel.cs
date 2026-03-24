namespace PlataformaELearning.Models.ViewModels
{
    public class PortalDashboardViewModel
    {
        public string CicloEscolarActual { get; set; }
        public string AvisoTitulo { get; set; }
        public string AvisoMensaje { get; set; }
        public int AvanceCuatrimestre { get; set; }
        public int TotalGruposAsignados { get; set; }
        public int TotalAlumnosAsignados { get; set; }
        public int TareasPendientes { get; set; }
        public int TotalAlumnos { get; set; }
        public int TotalDocentes { get; set; }
        public decimal UptimeSistema { get; set; }
    }
}