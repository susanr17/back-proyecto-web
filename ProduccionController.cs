using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using API_PANADERIA.Models;
using System.Data;
using System.Data.SqlClient;

namespace API_PANADERIA.Controllers
{
    [EnableCors("ReglasCors")]
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]

    public class ProduccionController : Controller
    {
        private readonly string cadenaSQL;
        public ProduccionController(IConfiguration config)
        {
            cadenaSQL = config.GetConnectionString("CadenaSQL");
        }

        [HttpGet]
        [Route("Lista")]
        public IActionResult Lista()
        {
            List<Produccion> lista = new List<Produccion>();

            try
            {
                using (var conexion = new SqlConnection(cadenaSQL))
                {
                    conexion.Open();
                    var cmd = new SqlCommand("SP_GET_PRODUCCION", conexion);
                    cmd.CommandType = CommandType.StoredProcedure;


                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            lista.Add(new Produccion
                            {
                                Id = Convert.ToInt32(rd["ID"]),
                                ClasePan = rd["CLASE_DE_PAN"].ToString(),
                                Libras = Convert.ToDecimal(rd["LIBRAS"]),
                                Bandejas = Convert.ToDecimal(rd["BANDEJAS"]),
                                Unidades = Convert.ToDecimal(rd["UNIDADES"]),
                                CostoUnidad = Convert.ToDecimal(rd["COSTO_POR_UNIDAD"]),
                                fechaRegistro = rd["FECHAREGISTRO"].ToString(),
                            });
                        }
                    }

                }



                return StatusCode(StatusCodes.Status200OK, new { response = lista });

            }
            catch (Exception error)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = error.Message, response = lista });
            }
        }


        [HttpPost]
        [Route("InsertarProduccion")]
        public IActionResult RegistrarProduccion(List<Produccion> produccionLista)
        {
             try
            {
                DataTable produccionTabla = new DataTable();
                produccionTabla.Columns.Add("CLASE_DE_PAN", typeof(string));
                produccionTabla.Columns.Add("LIBRAS", typeof(decimal));
                produccionTabla.Columns.Add("BANDEJAS", typeof(decimal));
                produccionTabla.Columns.Add("UNIDADES", typeof(decimal));
                produccionTabla.Columns.Add("COSTO_POR_UNIDAD", typeof(decimal));

              
                foreach (var produccion in produccionLista)
                {
                    produccionTabla.Rows.Add(
                        produccion.ClasePan, 
                        produccion.Libras,
                        produccion.Bandejas,
                        produccion.Unidades,
                        produccion.CostoUnidad);
                }


                string o_msgerror = "";
                string o_msg = "";

          
                using (var conexion = new SqlConnection(cadenaSQL))
                {
                
                    conexion.Open();

                
                    using (SqlCommand command = new SqlCommand("SP_REGISTRAR_PRODUCCION", conexion))
                    {
                       
                        command.CommandType = CommandType.StoredProcedure;


                        command.Parameters.AddWithValue("@ProduccionTabla", produccionTabla);
                        command.Parameters.Add("@o_msgerror", SqlDbType.VarChar, 200).Direction = ParameterDirection.Output;
                        command.Parameters.Add("@o_msg", SqlDbType.VarChar, 200).Direction = ParameterDirection.Output;

              
                        command.ExecuteNonQuery();

           
                        o_msgerror = command.Parameters["@o_msgerror"].Value.ToString();
                        o_msg = command.Parameters["@o_msg"].Value.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(o_msgerror))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = o_msgerror });
                }
                else
                {


                    return StatusCode(StatusCodes.Status200OK, new { mensaje = o_msg });
                }
            }
            catch (Exception ex)
            {
   
                return StatusCode(500, new { mensaje = "Error al registrar la producción: " + ex.Message });
            }
        }

        [HttpDelete]
        [Route("Eliminar/{idProduccion:int}")]
        public IActionResult Eliminar(int idProduccion)
        {
            try
            {

                using (var conexion = new SqlConnection(cadenaSQL))
                {
                    conexion.Open();
                    var cmd = new SqlCommand("SP_DELETE_PRODUCCION", conexion);
                    cmd.Parameters.AddWithValue("@i_id", idProduccion);

            
                    var outputMsgErrorParam = new SqlParameter("@o_msgerror", SqlDbType.VarChar, 200);
                    outputMsgErrorParam.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(outputMsgErrorParam);

                    var outputMsgParam = new SqlParameter("@o_msg", SqlDbType.VarChar, 200);
                    outputMsgParam.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(outputMsgParam);

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                    string msgError = outputMsgErrorParam.Value.ToString();
                    string msg = outputMsgParam.Value.ToString();

                    if (!string.IsNullOrEmpty(msgError))
                    {
                       
                        return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = msgError });
                    }

                  
                    return StatusCode(StatusCodes.Status200OK, new { mensaje = msg });
                }
            }
            catch (Exception error)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = error.Message });
            }
        }
    }
}
