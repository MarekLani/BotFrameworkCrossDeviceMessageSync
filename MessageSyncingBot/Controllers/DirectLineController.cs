using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MessageSyncingBot.Helpers;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace MessageSyncingBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DirectLineController : ControllerBase
    {
        IConfiguration configuration;
        public DirectLineController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpPost("token")]
        [EnableCors("AllowAllOrigins")]
        public async Task<IActionResult> PostAsync([FromBody]User user )
        {
            //Add Trusted Origin test
            //const origin = req.header('origin');
            //if (!trustedOrigin(origin))
            //{
            //    return res.send(403, 'not trusted origin');
            //}
            StringValues token;
            string res;
            
            try
            {
                if (Request.Query.TryGetValue("token", out token))
                {
                    res = await RenewDirectLineToken(token);
                }
                else
                {
                    res = await GenerateDirectLineToken(user.UserId);
                }
            }
            catch (Exception)
            {
               return BadRequest();
            }
            if (res != "")
                return Ok(res);
            else
                return BadRequest("Error obtaining token");
        }

        private async Task<string> GenerateDirectLineToken(string userId)
        {
            //var token = string.Empty;
            var clnt = new HttpClient();
            clnt.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration["DirectLineSecret"]);

            var rsp = await clnt.PostAsync("https://directline.botframework.com/v3/directline/tokens/generate", new StringContent($"{{ \"User\": {{ \"Id\": \"{userId}\" }} }}", Encoding.UTF8, "application/json"));

            if (rsp.IsSuccessStatusCode)
            {
                var str = rsp.Content.ReadAsStringAsync().Result;
                var obj = JsonConvert.DeserializeObject<DirectlineResponse>(str);
                //token = obj.token;
                ConversationSynchronizer.AddConversation(userId, obj.conversationId);
    
                return str;
            }
            return "";
        }

        private async Task<string> RenewDirectLineToken(string token)
        {
            //var token = string.Empty;
            var clnt = new HttpClient();
            clnt.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration["DirectLineSecret"]);

            var rsp = await clnt.PostAsync("https://directline.botframework.com/v3/directline/tokens/refresh", new StringContent(string.Empty, Encoding.UTF8, "application/json"));

            if (rsp.IsSuccessStatusCode)
            {
                var str = rsp.Content.ReadAsStringAsync().Result;
                //var obj = JsonConvert.DeserializeObject<DirectlineResponse>(str);
                //token = obj.token;
                return str;
            }
            return "";
        }
    }

    class DirectlineResponse
    {
        public string conversationId { get; set; }
        public string token { get; set; }
        public int expires_in { get; set; }
    }

    public class User
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }
    }
}