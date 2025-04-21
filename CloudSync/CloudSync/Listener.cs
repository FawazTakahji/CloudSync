using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace CloudSync;

public class Listener
{
    private readonly HttpListener _listener;

    public Listener(string prefix)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
    }

    public void Stop()
    {
        _listener.Stop();
    }

    public async Task<NameValueCollection?> ListenAsync()
    {
        _listener.Start();

        HttpListenerContext context;
        try
        {
            context = await _listener.GetContextAsync();
        }
        catch (HttpListenerException ex)
        {
            if (ex.ErrorCode == 995)
            {
                return null;
            }

            throw;
        }
        NameValueCollection query = context.Request.QueryString;

        string html = "<html> <head><title>Authorization Complete</title></head> <body><h1>Authorization Complete</h1> <h2>You can close this tab.</h2></body> </html>";
        byte[] buffer = Encoding.UTF8.GetBytes(html);
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.OutputStream.Close();
        context.Response.Close();

        return query;
    }
}