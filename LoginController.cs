using Microsoft.AspNetCore.Mvc;


using API_PANADERIA.Models;

using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

using System.Data.SqlClient;
using System.Data;

using System.Text;

namespace API_PANADERIA.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {

        private readonly string secretKey;
        private readonly string cadenaSQL;

        public LoginController(IConfiguration config)
        {
            secretKey = config.GetSection("settings").GetSection("secretKey").ToString();
            cadenaSQL = config.GetConnectionString("CadenaSQL");
        }

        [HttpPost]
        [Route("Login")]
        public IActionResult Validar([FromBody] Usuario request)
        {
            string mensajeError = "";
            string mensajeGeneral = "";
            string rolUsuario = "";
            string idUsuario = "";

            try
            {

                var contraseñaDecodificada = Convert.FromBase64String(request.Contrasenia);
              
                using (var conexion = new SqlConnection(cadenaSQL))
                {
                    conexion.Open();
                    var cmd = new SqlCommand("SP_LOGIN_USUARIO", conexion);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@i_nombre_usuario", request.idUsuario);
                    cmd.Parameters.AddWithValue("@i_contraseña", contraseñaDecodificada);
                    cmd.Parameters.Add("@o_msgerror", SqlDbType.VarChar, 200).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@o_msg", SqlDbType.VarChar, 200).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@o_rol", SqlDbType.VarChar, 50).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("@o_username", SqlDbType.VarChar, 50).Direction = ParameterDirection.Output;
                    cmd.ExecuteNonQuery();

                    mensajeError = cmd.Parameters["@o_msgerror"].Value.ToString();
                    mensajeGeneral = cmd.Parameters["@o_msg"].Value.ToString();
                    rolUsuario = cmd.Parameters["@o_rol"].Value.ToString();
                    idUsuario = cmd.Parameters["@o_username"].Value.ToString();

                }

                if (string.IsNullOrEmpty(mensajeError))
                {
                    var keyBytes = Encoding.ASCII.GetBytes(secretKey);
                    var claims = new ClaimsIdentity();
                    claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, request.idUsuario));
                    claims.AddClaim(new Claim(ClaimTypes.Role, rolUsuario));
                    claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, idUsuario));

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = claims,
                        Expires = DateTime.UtcNow.AddMinutes(100),
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
                    };

                    var tokenHandler = new JwtSecurityTokenHandler();
                    var tokenConfig = tokenHandler.CreateToken(tokenDescriptor);

                    string tokencreado = tokenHandler.WriteToken(tokenConfig);

                    return StatusCode(StatusCodes.Status200OK, new { mensaje = mensajeGeneral, token = tokencreado, rol = rolUsuario, username = idUsuario });
                }
                else
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, new { mensaje = mensajeError, token = "", rol = "", username = "" });
                }
            }
            catch (Exception error)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = error.Message, token = "", rol = "", username = "" });
            }
        }



    }
}
