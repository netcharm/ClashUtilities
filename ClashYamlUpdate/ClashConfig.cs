using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Clash
{
    public partial class ConfigYaml
    {
        public static ConfigYaml FromFile(string file)
        {
            ConfigYaml result = null;
            if (File.Exists(file))
            {
                try
                {
                    var yaml = new Deserializer();
                    var contents = File.ReadAllLines(file, Encoding.UTF8).ToList();
                    if (!contents[0].StartsWith("#")) contents.Insert(0, "# YAML Starting...");
                    result = yaml.Deserialize<ConfigYaml>(string.Join(Environment.NewLine, contents));
                }
                catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
            }
            return (result);
        }

        public static ConfigYaml FromStream(Stream stream)
        {
            ConfigYaml result = null;
            if (stream is Stream && stream.CanRead && stream.Length > 0)
            {
                try
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    var yaml = new Deserializer();
                    var bytes = new byte[stream.Length];
                    var count = stream.Read(bytes, 0, (int)stream.Length);
                    var contents = Encoding.UTF8.GetString(bytes);
                    if (!contents[0].Equals('#')) contents.Insert(0, $"# YAML Starting...{Environment.NewLine}");
                    result = yaml.Deserialize<ConfigYaml>(string.Join(Environment.NewLine, contents));
                }
                catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
            }
            return (result);
        }

        public static async Task<ConfigYaml> FromStreamAsync(Stream stream)
        {
            ConfigYaml result = null;
            if (stream is Stream && stream.CanRead && stream.Length > 0)
            {
                try
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    var yaml = new Deserializer();
                    var bytes = new byte[stream.Length];
                    var count = await stream.ReadAsync(bytes, 0, (int)stream.Length);
                    var contents = Encoding.UTF8.GetString(bytes);
                    if (!contents[0].Equals('#')) contents.Insert(0, $"# YAML Starting...{Environment.NewLine}");
                    result = yaml.Deserialize<ConfigYaml>(string.Join(Environment.NewLine, contents));
                }
                catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
            }
            return (result);
        }

        public void ToFile(string file)
        {
            try
            {
                //var yaml = new Serializer();
                var yaml = new SerializerBuilder().WithIndentedSequences().Build();
                var contents = yaml.Serialize(this);
                File.WriteAllText(file, contents, Encoding.UTF8);
            }
            catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
        }

        public void ToStream(Stream stream)
        {
            try
            {
                if (stream is Stream && stream.CanWrite)
                {
                    var yaml = new SerializerBuilder().WithIndentedSequences().Build();
                    var contents = yaml.Serialize(this);
                    var bytes = Encoding.UTF8.GetBytes(contents);

                    stream.Seek(0, SeekOrigin.Begin);
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
        }

        public async void ToStreamAsync(Stream stream)
        {
            try
            {
                if (stream is Stream && stream.CanWrite)
                {
                    var yaml = new SerializerBuilder().WithIndentedSequences().Build();
                    var contents = yaml.Serialize(this);
                    var bytes = Encoding.UTF8.GetBytes(contents);

                    stream.Seek(0, SeekOrigin.Begin);
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
        }

        public ConfigYaml MergeTo(ConfigYaml dst)
        {
            var result = dst;

            #region Merge Proxy Providers
            if (this.ProxyProviders != null)
            {
                if (result.ProxyProviders == null)
                {
                    result.ProxyProviders = this.ProxyProviders;
                }
                else
                {
                    foreach (var provider in this.ProxyProviders)
                    {
                        if (result.ProxyProviders.ContainsKey(provider.Key)) continue;
                        result.ProxyProviders.Add(provider.Key, provider.Value);
                    }
                }
            }
            #endregion

            #region Merge Rule Providers
            if (this.RuleProviders != null)
            {
                if (result.RuleProviders == null)
                {
                    result.RuleProviders = this.RuleProviders;
                }
                else
                {
                    foreach (var provider in this.RuleProviders)
                    {
                        if (result.RuleProviders.ContainsKey(provider.Key)) continue;
                        result.RuleProviders.Add(provider.Key, provider.Value);
                    }
                }
            }
            #endregion

            #region Merge Proxies
            var proxy_list = dst.Proxies.ToList();
            var proxy_names = proxy_list.Select(p => p.Name);
            var proxy_new = new List<string>();
            foreach (var proxy in this.Proxies)
            {
                if (proxy_names.Contains(proxy.Name)) continue;
                //proxy_list.Append()
                proxy_list.Add(proxy);
                proxy_new.Add(proxy.Name);
            }
            result.Proxies = proxy_list.ToArray();
            #endregion

            #region Merge Proxy Groups
            var group_list = dst.ProxyGroups.ToList();
            var group_names = group_list.Select(g => g.Name);
            var group_new = new List<ProxyGroup>();
            foreach (var group in this.ProxyGroups)
            {
                if (group_names.Contains(group.Name)) continue;
                //proxy_list.Append()
                group_list.Insert(0, group);
                group_new.Add(group);
            }
            var group_new_names = group_new.Select(g => g.Name);
            foreach (var group in group_list)
            {
                if (!group.Type.Equals("select", StringComparison.CurrentCultureIgnoreCase)) continue;
                if (group_new_names.Contains(group.Name)) continue;
                var group_proxy = group.Proxies.ToList();
                foreach (var gn in group_new)
                {
                    if (group.Proxies.Contains(gn.Name)) continue;
                    if (gn.Proxies.Contains(group.Name)) continue;
                    group_proxy.Insert(0, gn.Name);
                }
                group.Proxies = group_proxy.ToArray();
            }
            result.ProxyGroups = group_list.ToArray();
            #endregion

            return (result);
        }

        /// <summary>
        /// 
        /// </summary>
        public enum Network { Http, Ws };
        /// <summary>
        /// 
        /// </summary>
        public enum ProxyType { HTTP, Socks5, Shadowsocks, ShadowsocksR, Trojan, Vmess, Snell };
        /// <summary>
        /// 
        /// </summary>
        public enum ProxyGroupType { Relay, Select, UrlTest, FallBack, LoadBalance };

        /// <summary>
        /// Port of HTTP(S) proxy server on the local end
        /// </summary>
        [YamlMember(Alias = "port", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public int? Port { get; set; }

        /// <summary>
        /// Port of SOCKS5 proxy server on the local end
        /// </summary>
        [YamlMember(Alias = "socks-port", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public int? SocksPort { get; set; }

        /// <summary>
        /// Transparent proxy server port for Linux and macOS
        /// </summary>
        [YamlMember(Alias = "redir-port", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public int? RedirPort { get; set; }

        /// <summary>
        /// HTTP(S) and SOCKS5 server on the same port
        /// </summary>
        [YamlMember(Alias = "mixed-port", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public int? MixedPort { get; set; }

        /// <summary>
        /// authentication of local SOCKS5/HTTP(S) server
        /// </summary>
        [YamlMember(Alias = "authentication", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string[] Authentication { get; set; }

        /// <summary>
        /// Set to true to allow connections to local-end server from
        /// other LAN IP addresses
        /// </summary>
        [YamlMember(Alias = "allow-lan", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? AllowLan { get; set; }

        /// <summary>
        /// This is only applicable when `allow-lan` is `true`
        /// '*': bind all IP addresses
        /// 192.168.122.11: bind a single IPv4 address
        /// "[aaaa::a8aa:ff:fe09:57d8]": bind a single IPv6 address
        /// </summary>
        [YamlMember(Alias = "bind-address", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string BindAddress { get; set; }

        /// <summary>
        /// Clash router working mode
        /// rule: rule-based packet routing
        /// global: all packets will be forwarded to a single endpoint
        /// direct: directly forward the packets to the Internet
        /// </summary>
        [YamlMember(Alias = "mode", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Mode { get; set; }

        /// <summary>
        /// Clash by default prints logs to STDOUT
        /// info / warning / error / debug / silent
        /// </summary>
        [YamlMember(Alias = "log-level", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string LogLevel { get; set; }

        /// <summary>
        /// When set to false, resolver won't translate hostnames to IPv6 addresses
        /// </summary>
        [YamlMember(Alias = "ipv6", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? IPv6 { get; set; }

        /// <summary>
        /// RESTful web API listening address
        /// </summary>
        [YamlMember(Alias = "external-controller", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string ExternalController { get; set; }

        /// <summary>
        /// A relative path to the configuration directory or an absolute path to a
        /// directory in which you put some static web resource. Clash core will then
        /// serve it at `${API}/ui`.
        /// </summary>
        [YamlMember(Alias = "external-ui", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string ExternalUi { get; set; }

        /// <summary>
        /// Secret for the RESTful API (optional)
        /// Authenticate by spedifying HTTP header `Authorization: Bearer ${secret}`
        /// ALWAYS set a secret if RESTful API is listening on 0.0.0.0
        /// </summary>
        [YamlMember(Alias = "secret", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Secret { get; set; }

        /// <summary>
        /// Outbound interface name
        /// </summary>
        [YamlMember(Alias = "interface-name", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string InterfaceName { get; set; }

        /// <summary>
        /// Static hosts for DNS server and connection establishment, only works
        /// when `dns.enhanced-mode` is `redir-host`.
        /// 
        /// Wildcard hostnames are supported (e.g. *.clash.dev, *.foo.*.example.com)
        /// Non-wildcard domain names has a higher priority than wildcard domain names
        /// e.g. foo.example.com > *.example.com > .example.com
        /// P.S. +.foo.com equals to .foo.com and foo.com
        /// </summary>
        [YamlMember(Alias = "hosts", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public KeyValuePair<string, string>[] Hosts { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [YamlMember(Alias = "profile", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public Profile Profile { get; set; }

        /// <summary>
        /// DNS server settings
        /// This section is optional. When not present, DNS server will be disabled.
        /// </summary>
        [YamlMember(Alias = "dns", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public Dns Dns { get; set; }

        [YamlMember(Alias = "proxy-providers", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public Dictionary<string, Provider> ProxyProviders { get; set; }

        [YamlMember(Alias = "rule-providers", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public Dictionary<string, Provider> RuleProviders { get; set; }

        [YamlMember(Alias = "proxies", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public Proxy[] Proxies { get; set; }

        [YamlMember(Alias = "proxy-groups", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public ProxyGroup[] ProxyGroups { get; set; }

        [YamlMember(Alias = "rules", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string[] Rules { get; set; }
    }

    public partial class Profile
    {
        /// <summary>
        /// Store the `select` results in $HOME/.config/clash/.cache
        /// set false If you don't want this behavior
        /// when two different configurations have groups with the same name, the selected values are shared
        /// </summary>
        [YamlMember(Alias = "store-selected", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool StoreSelected { get; set; }

        /// <summary>
        /// persistence fakeip
        /// </summary>
        [YamlMember(Alias = "store-fake-ip", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool StoreFakeIp { get; set; }
    }

    /// <summary>
    /// DNS server settings
    /// This section is optional. When not present, the DNS server will be disabled.
    /// </summary>
    public partial class Dns
    {
        [YamlMember(Alias = "enable", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool Enable { get; set; }

        [YamlMember(Alias = "listen", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Listen { get; set; }

        /// <summary>
        /// when the false, response to AAAA questions will be empty
        /// </summary>
        [YamlMember(Alias = "ipv6", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool IPv6 { get; set; }

        /// <summary>
        /// fake-ip or redir-host (not recommended)
        /// </summary>
        [YamlMember(Alias = "enhanced-mode", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string EnhancedMode { get; set; }

        [YamlMember(Alias = "fake-ip-range", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string FakeIPRange { get; set; }

        /// <summary>
        /// Hostnames in this list will not be resolved with fake IPs
        /// i.e. questions to these domain names will always be answered with their
        /// real IP addresses
        /// </summary>
        [YamlMember(Alias = "fake-ip-filter", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string[] FakeIPFilter { get; set; }

        /// <summary>
        /// lookup hosts and return IP record
        /// </summary>
        [YamlMember(Alias = "use-hosts", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool UseHosts { get; set; }

        /// <summary>
        /// These nameservers are used to resolve the DNS nameserver hostnames below.
        /// Specify IP addresses only
        /// </summary>
        [YamlMember(Alias = "default-nameserver", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string[] DefaultNameServer { get; set; }

        /// <summary>
        /// Supports UDP, TCP, DoT, DoH. You can specify the port to connect to.
        /// All DNS questions are sent directly to the nameserver, without proxies
        /// involved. Clash answers the DNS question with the first result gathered.
        /// </summary>
        [YamlMember(Alias = "nameserver", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public Uri[] NameServer { get; set; }

        /// <summary>
        /// Lookup domains via specific nameservers
        /// </summary>
        [YamlMember(Alias = "nameserver-policy", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public KeyValuePair<string, string>[] NameserverPolicy { get; set; }

        /// <summary>
        /// When `fallback` is present, the DNS server will send concurrent requests
        /// to the servers in this section along with servers in `nameservers`.
        /// The answers from fallback servers are used when the GEOIP country
        /// is not `CN`.
        /// </summary>
        [YamlMember(Alias = "fallback", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public Uri[] Fallback { get; set; }

        /// <summary>
        /// If IP addresses resolved with servers in `nameservers` are in the specified
        /// subnets below, they are considered invalid and results from `fallback`
        /// servers are used instead.
        ///
        /// IP address resolved with servers in `nameserver` is used when
        /// `fallback-filter.geoip` is true and when GEOIP of the IP address is `CN`.
        /// 
        /// If `fallback-filter.geoip` is false, results from `nameserver` nameservers
        /// are always used if not match `fallback-filter.ipcidr`.
        /// 
        /// This is a countermeasure against DNS pollution attacks.
        /// </summary>
        [YamlMember(Alias = "fallback-filter", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public FallbackFilter FallbackFilter { get; set; }
    }

    /// <summary>
    /// If IP addresses resolved with servers in `nameservers` are in the specified
    /// subnets below, they are considered invalid and results from `fallback`
    /// servers are used instead.
    ///
    /// IP address resolved with servers in `nameserver` is used when
    /// `fallback-filter.geoip` is true and when GEOIP of the IP address is `CN`.
    /// 
    /// If `fallback-filter.geoip` is false, results from `nameserver` nameservers
    /// are always used if not match `fallback-filter.ipcidr`.
    /// 
    /// This is a countermeasure against DNS pollution attacks.
    /// </summary>
    public partial class FallbackFilter
    {
        [YamlMember(Alias = "geoip", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool GeoIP { get; set; }

        [YamlMember(Alias = "geoip-code", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string GeoIPCode { get; set; }

        [YamlMember(Alias = "ipcidr", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string[] IPCidr { get; set; }

        [YamlMember(Alias = "domain", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string[] Domain { get; set; }
    }

    public partial class Proxy
    {
        /// <summary>
        ///   Shadowsocks
        ///   The supported ciphers (encryption methods):
        ///     aes-128-gcm aes-192-gcm aes-256-gcm
        ///     aes-128-cfb aes-192-cfb aes-256-cfb
        ///     aes-128-ctr aes-192-ctr aes-256-ctr
        ///     rc4-md5 chacha20-ietf xchacha20
        ///     chacha20-ietf-poly1305 xchacha20-ietf-poly1305
        ///     
        ///   vmess
        ///   cipher support auto/aes-128-gcm/chacha20-poly1305/none
        ///   
        ///   ShadowsocksR
        ///   The supported ciphers (encryption methods): all stream ciphers in ss
        ///   The supported obfses:
        ///     plain http_simple http_post
        ///       random_head tls1.2_ticket_auth tls1.2_ticket_fastauth
        ///   The supported supported protocols:
        ///     origin auth_sha1_v4 auth_aes128_md5
        ///     auth_aes128_sha1 auth_chain_a auth_chain_b 
        /// </summary>
        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "server")]
        public string Server { get; set; }

        [YamlMember(Alias = "port")]
        public long Port { get; set; }

        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlMember(Alias = "cipher", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Cipher { get; set; }

        [YamlMember(Alias = "tls", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? Tls { get; set; }

        [YamlMember(Alias = "uuid", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public Guid? Uuid { get; set; }

        [YamlMember(Alias = "alterId", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public long? AlterId { get; set; }

        [YamlMember(Alias = "skip-cert-verify", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? SkipCertVerify { get; set; }

        [YamlMember(Alias = "udp", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool? Udp { get; set; }

        [YamlMember(Alias = "network", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Network { get; set; }

        [YamlMember(Alias = "servername", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string ServerName { get; set; }

        [YamlMember(Alias = "username", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Username { get; set; }

        [YamlMember(Alias = "password", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Password { get; set; }

        [YamlMember(Alias = "h2-opts", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public H2Opts H2Opts { get; set; }

        [YamlMember(Alias = "http-opts", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public HttpOpts HttpOpts { get; set; }

        [YamlMember(Alias = "ws-path", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string WsPath { get; set; }

        [YamlMember(Alias = "ws-headers", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public WsHeaders WsHeaders { get; set; }

        [YamlMember(Alias = "version", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public long Version { get; set; }

        [YamlMember(Alias = "psk", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Psk { get; set; }

        [YamlMember(Alias = "plugin", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Plugin { get; set; }

        [YamlMember(Alias = "sni", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Sni { get; set; }

        [YamlMember(Alias = "obfs", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Obfs { get; set; }

        [YamlMember(Alias = "obfs-param", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string ObfsParams { get; set; }
        
        [YamlMember(Alias = "obfs-opts", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public ObfsOpts ObfsOpts { get; set; }

        [YamlMember(Alias = "protocol", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Protocol { get; set; }

        [YamlMember(Alias = "protocol-param", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string ProtocolParam { get; set; }

        [YamlMember(Alias = "grpc-opts", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public GrpcOpts GrpcOpts { get; set; }
    }

    public partial class ObfsOpts
    {
        [YamlMember(Alias = "mode", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Mode { get; set; }

        [YamlMember(Alias = "host", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Host { get; set; }
    }

    public partial class GrpcOpts
    {
        [YamlMember(Alias = "grpc-service-name", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string GrpcServiceName { get; set; }
    }

    public partial class WsOpts
    {
        [YamlMember(Alias = "path", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Path { get; set; }

        [YamlMember(Alias = "headers", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public WsHeaders Headers { get; set; }
    }

    public partial class WsHeaders
    {
        [YamlMember(Alias = "Host", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Host { get; set; }
    }

    public partial class H2Opts
    {
        [YamlMember(Alias = "host", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string[] Host { get; set; }

        [YamlMember(Alias = "path", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Path { get; set; }
    }

    public partial class HttpOpts
    {
        [YamlMember(Alias = "method", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Method { get; set; }

        [YamlMember(Alias = "path", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string[] Path { get; set; }

        [YamlMember(Alias = "headers", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public HttpOptsHeaders Headers { get; set; }
    }

    public partial class HttpOptsHeaders
    {
        [YamlMember(Alias = "Connection", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string[] Connection { get; set; }
    }

    public partial class PluginOpts
    {
        [YamlMember(Alias = "mode", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Mode { get; set; }

        [YamlMember(Alias = "tls", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool TLS { get; set; }

        [YamlMember(Alias = "skip-cert-verify", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool SkipCertVerify { get; set; }

        [YamlMember(Alias = "host", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Host { get; set; }

        [YamlMember(Alias = "path", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Path { get; set; }

        [YamlMember(Alias = "mux", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool Mux { get; set; }

        [YamlMember(Alias = "headers", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public PluginOptsHeaders Headers { get; set; }
    }

    public partial class PluginOptsHeaders
    {
        [YamlMember(Alias = "custom", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Custom { get; set; }
    }

    public partial class ProxyGroup
    {
        [YamlMember(Alias = "name")]
        public string Name { get; set; }

        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlMember(Alias = "url", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Url { get; set; }

        [YamlMember(Alias = "interval", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public int? Interval { get; set; }

        [YamlMember(Alias = "use", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string[] Use { get; set; }

        [YamlMember(Alias = "proxies", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string[] Proxies { get; set; }
    }

    public partial class Provider
    {
        [YamlMember(Alias = "type")]
        public string Type { get; set; }

        [YamlMember(Alias = "url", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Url { get; set; }

        [YamlMember(Alias = "path")]
        public string Path { get; set; }

        [YamlMember(Alias = "behavior", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Behavior { get; set; }

        [YamlMember(Alias = "interval", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public long? Interval { get; set; }

        [YamlMember(Alias = "health-check", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public HealthCheck HealthCheck { get; set; }
    }

    public partial class HealthCheck
    {
        [YamlMember(Alias = "enable", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool Enable { get; set; }

        [YamlMember(Alias = "interval", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public long Interval { get; set; }

        [YamlMember(Alias = "lazy", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public bool Lazy { get; set; }

        [YamlMember(Alias = "url", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string Url { get; set; }
    }

}
