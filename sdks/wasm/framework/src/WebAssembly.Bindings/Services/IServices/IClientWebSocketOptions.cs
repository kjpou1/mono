using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace WebAssembly.Services.IServices {
	public interface IClientWebSocketOptions {
		X509CertificateCollection ClientCertificates { get; set; }
		CookieContainer Cookies { get; set; }
                ICredentials Credentials { get; set; }
		TimeSpan KeepAliveInterval { get; set; }
                IWebProxy Proxy { get; set; }
                RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }
                bool UseDefaultCredentials { get; set; }
                void AddSubProtocol (string subProtocol);
                IList<string> RequestedSubProtocols { get; }
                void SetBuffer (int receiveBufferSize, int sendBufferSize);
                void SetBuffer (int receiveBufferSize, int sendBufferSize, System.ArraySegment<byte> buffer);
                void SetRequestHeader (string headerName, string headerValue);
                void SetToReadOnly ();
        }
}
