using System;
using System.Collections.Generic;

namespace Gbf.Models
{
    public class Incidente
    {
        public int Id { get; set; }

        public string Titulo { get; set; }
        public string Descripcion { get; set; }

        public string Estado { get; set; }

        public int EmpresaId { get; set; }
        public Empresa Empresa { get; set; }

        public int VehiculoId { get; set; }
        public Vehiculo Vehiculo { get; set; }

        public int ConductorId { get; set; }
        public Conductor Conductor { get; set; }

        public DateTime FechaCreacion { get; set; }

        // 🔥 ESTA LÍNEA SOLUCIONA TODO
        public List<IncidentePioneta> IncidentePionetas { get; set; }
    }
}