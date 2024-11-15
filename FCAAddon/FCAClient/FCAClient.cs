using System.Text;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.Runtime;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace FCAUconnect;

public enum FcaBrand
{
  Fiat,
  Jeep,
  AlfaRomeo,
  Debug
}

public enum FcaRegion
{
  Europe,
  US
}

public class FCAClient 
{
  private readonly string _loginApiKey = "";
  private readonly string _apiKey= "";
  private readonly string _loginUrl= "";
  private readonly string _tokenUrl= "";
  private readonly string _apiUrl= "";
  private readonly string _authApiKey= ""; // for pin
  private readonly string _authUrl= ""; // for pin
  private readonly string _locale = ""; // for pin
  private readonly RegionEndpoint _awsEndpoint = RegionEndpoint.EUWest1; 
  
  private readonly string _user;
  private readonly string _password;
  private readonly CookieJar _cookieJar = new();

  private readonly FcaBrand _brand;
  private readonly FcaRegion _region;
  
  private readonly IFlurlClient _defaultHttpClient;

  private (string userUid, ImmutableCredentials awsCredentials)? _loginInfo = null;

  public FCAClient(string user, string password, FcaBrand brand = FcaBrand.Fiat, FcaRegion region = FcaRegion.Europe)
  {
    _user = user;
    _password = password;
    _brand = brand;
    _region = region;

    if (_region == FcaRegion.Europe)
    {
      _loginApiKey = "3_mOx_J2dRgjXYCdyhchv3b5lhi54eBcdCTX4BI8MORqmZCoQWhA0mV2PTlptLGUQI";

      _apiKey = "qLYupk65UU1tw2Ih1cJhs4izijgRDbir2UFHA3Je";
      _tokenUrl = "https://authz.sdpr-01.fcagcv.com/v2/cognito/identity/token";
      _apiUrl = "https://channels.sdpr-01.fcagcv.com";

      _authApiKey = "JWRYW7IYhW9v0RqDghQSx4UcRYRILNmc8zAuh5ys"; // for pin
      _authUrl = "https://mfa.fcl-01.fcagcv.com"; // for pin

      _locale = "de_de"; // for pin
      _awsEndpoint = RegionEndpoint.EUWest1; 
    }
    else
    {
      _loginApiKey = "3_FSxGyaktviayTDRcgp9r9o2KjuFSrHT13wWNN9zPrvAGUCoXPDqoIPOwlBUhck4A";

       _apiKey = "OgNqp2eAv84oZvMrXPIzP8mR8a6d9bVm1aaH9LqU";
      _tokenUrl = "https://authz.sdpr-02.fcagcv.com/v2/cognito/identity/token";
      _apiUrl = "https://channels.sdpr-02.fcagcv.com";

      _authApiKey = "fNQO6NjR1N6W0E5A6sTzR3YY4JGbuPv48Nj9aZci"; // UNKNOWN
      _authUrl = "https://mfa.fcl-02.fcagcv.com"; // UNKNOWN

      _locale = "en_us";
      _awsEndpoint = RegionEndpoint.USEast1;
    }

    if (_brand == FcaBrand.Debug)
    {
      _loginApiKey = "3_etlYkCXNEhz4_KJVYDqnK1CqxQjvJStJMawBohJU2ch3kp30b0QCJtLCzxJ93N-M";
       _loginUrl = "https://login-us.alfaromeo.com";
      _apiKey = "2wGyL6PHec9o1UeLPYpoYa1SkEWqeBur9bLsi24i";
      _tokenUrl = "https://authz.sdpr-01.fcagcv.com/v2/cognito/identity/token";
      _apiUrl = "https://channels.sdpr-01.fcagcv.com";
      _authApiKey = "JWRYW7IYhW9v0RqDghQSx4UcRYRILNmc8zAuh5ys"; // for pin
      _authUrl = "https://mfa.fcl-01.fcagcv.com"; // for pin
       _locale = "en_us";
      _awsEndpoint = RegionEndpoint.USEast1;
    }
    else if (_brand == FcaBrand.Fiat)
    {
      if (_region == FcaRegion.Europe)
      {
        _loginUrl = "https://loginmyuconnect.fiat.com";
      }
      else
      {
        _loginUrl = "https://login-us.fiat.com";
      }
    }
    else if (_brand == FcaBrand.AlfaRomeo)
    {
      if (_region == FcaRegion.Europe)
      {
        _loginUrl = "https://login.alfaromeo.com";
      }
      else
      {
       _loginUrl = "https://login-us.alfaromeo.com";      
      }
    }
    else if (_brand == FcaBrand.Jeep)
    {
      if (_region == FcaRegion.Europe)
      {
        _loginUrl = "https://login.jeep.com";
      }
      else
      {
        _loginUrl = "https://login-us.jeep.com";
      }
    }

    
    _defaultHttpClient = new FlurlClient().Configure(settings =>
    {
      settings.HttpClientFactory = new PollyHttpClientFactory();
    });
  }

  public async Task LoginAndKeepSessionAlive()
  {
    if (_loginInfo is not null)
      return;
    
    await this.Login();
    
    _ = Task.Run(async () =>
    {
      var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));
      
      while (await timer.WaitForNextTickAsync())
      {
        try
        {
          Log.Information("Refresh Session");
          await this.Login();
        }
        catch (Exception e)
        {
          
          Log.Error("ERROR WHILE REFRESH SESSION");
          Log.Debug("login {0}", e);
        }
      }
    });
  }

  private async Task Login()
  {
    var loginResponse = await _loginUrl
      .WithClient(_defaultHttpClient)
      .AppendPathSegment("accounts.webSdkBootstrap")
      .SetQueryParam("apiKey", _loginApiKey)
      .WithCookies(_cookieJar)
      .GetJsonAsync<FiatLoginResponse>();

    Log.Debug("loginResponse: {0}", loginResponse.Dump());

    loginResponse.ThrowOnError("Login failed.");

    var authResponse = await _loginUrl
      .WithClient(_defaultHttpClient)
      .AppendPathSegment("accounts.login")
      .WithCookies(_cookieJar)
      .PostUrlEncodedAsync(
        WithFiatDefaultParameter(new()
        {
          { "loginID", _user },
          { "password", _password },
          { "sessionExpiration", TimeSpan.FromMinutes(5).TotalSeconds },
          { "include", "profile,data,emails,subscriptions,preferences" },
        }))
      .ReceiveJson<FiatAuthResponse>();

    Log.Debug("authResponse : {0}", authResponse.Dump());

    authResponse.ThrowOnError("Authentication failed.");

    var jwtResponse = await _loginUrl
      .WithClient(_defaultHttpClient)
      .AppendPathSegment("accounts.getJWT")
      .SetQueryParams(
        WithFiatDefaultParameter(new()
        {
          { "fields", "profile.firstName,profile.lastName,profile.email,country,locale,data.disclaimerCodeGSDP" },
          { "login_token", authResponse.SessionInfo.LoginToken }
        }))
      .WithCookies(_cookieJar)
      .GetJsonAsync<FiatJwtResponse>();

    Log.Debug("jwtResponse : {0}", jwtResponse.Dump());

    jwtResponse.ThrowOnError("Authentication failed.");

    var identityResponse = await _tokenUrl
      .WithClient(_defaultHttpClient)
      .WithHeader("content-type", "application/json")
      .WithHeaders(WithAwsDefaultParameter(_apiKey))
      .PostJsonAsync(new
      {
        gigya_token = jwtResponse.IdToken,
      })
      .ReceiveJson<FcaIdentityResponse>();

    Log.Debug("identityResponse : {0}", identityResponse.Dump());
    
    identityResponse.ThrowOnError("Identity failed.");

    var client = new AmazonCognitoIdentityClient(new AnonymousAWSCredentials(), _awsEndpoint);

    var res = await client.GetCredentialsForIdentityAsync(identityResponse.IdentityId,
      new Dictionary<string, string>()
      {
        { "cognito-identity.amazonaws.com", identityResponse.Token }
      });

    _loginInfo = (authResponse.UID, new ImmutableCredentials(res.Credentials.AccessKeyId,
      res.Credentials.SecretKey,
      res.Credentials.SessionToken));
  }

  private Dictionary<string, object> WithAwsDefaultParameter(string apiKey, Dictionary<string, object>? parameters = null)
  {
    var dict = new Dictionary<string, object>()
    {
      { "x-clientapp-name", "CWP" },
      { "x-clientapp-version", "1.0" },
      { "clientrequestid", Guid.NewGuid().ToString("N")[..16] },
      { "x-api-key", apiKey },
      { "locale", _locale },
      { "x-originator-type", "web" },
    };

    foreach (var parameter in parameters ?? new())
      dict.Add(parameter.Key, parameter.Value);

    return dict;
  }

  private Dictionary<string, object> WithFiatDefaultParameter(Dictionary<string, object>? parameters = null)
  {
    var dict = new Dictionary<string, object>()
    {
      { "targetEnv", "jssdk" },
      { "loginMode", "standard" },
      { "sdk", "js_latest" },
      { "authMode", "cookie" },
      { "sdkBuild", "12234" },
      { "format", "json" },
      { "APIKey", _loginApiKey },
    };

    foreach (var parameter in parameters ?? new())
      dict.Add(parameter.Key, parameter.Value);

    return dict;
  }
  
  public async Task SendCommand(string vin, string command, string pin, string action)
  {
    ArgumentNullException.ThrowIfNull(_loginInfo);
    
    var (userUid, awsCredentials) = _loginInfo.Value;

    var data = new
    {
      pin = Convert.ToBase64String(Encoding.UTF8.GetBytes(pin))
    };

    var pinAuthResponse = await _authUrl
      .AppendPathSegments("v1", "accounts", userUid, "ignite", "pin", "authenticate")
      .WithHeaders(WithAwsDefaultParameter(_authApiKey))
      .AwsSign(awsCredentials, _awsEndpoint, data)
      .PostJsonAsync(data)
      .ReceiveJson<FcaPinAuthResponse>();

    Log.Debug("pinAuthResponse: {0}", pinAuthResponse.Dump());

    var json = new
    {
      command, 
      pinAuth = pinAuthResponse.Token
    };

    var commandResponse = await _apiUrl
      .AppendPathSegments("v1", "accounts", userUid, "vehicles", vin, action)
      .WithHeaders(WithAwsDefaultParameter(_apiKey))
      .AwsSign(awsCredentials, _awsEndpoint, json)
      .PostJsonAsync(json)
      .ReceiveJson<FcaCommandResponse>();

    Log.Debug("commandResponse: {0}", commandResponse.Dump());
  }

  public async Task<Vehicle[]> Fetch()
  {
    ArgumentNullException.ThrowIfNull(_loginInfo);

    var (userUid, awsCredentials) = _loginInfo.Value;

    var vehicleResponse = await _apiUrl
      .WithClient(_defaultHttpClient)
      .AppendPathSegments("v4", "accounts", userUid, "vehicles")
      .SetQueryParam("stage", "ALL")
      .WithHeaders(WithAwsDefaultParameter(_apiKey))
      .AwsSign(awsCredentials, _awsEndpoint)
      .GetJsonAsync<VehicleResponse>();

    Log.Debug("vehicleResponse: {0}", vehicleResponse.Dump());

    foreach (var vehicle in vehicleResponse.Vehicles)
    {
      var vehicleDetails = await _apiUrl
        .WithClient(_defaultHttpClient)
        .AppendPathSegments("v2", "accounts", userUid, "vehicles", vehicle.Vin, "status")
        .WithHeaders(WithAwsDefaultParameter(_apiKey))
        .AwsSign(awsCredentials, _awsEndpoint)
        .GetJsonAsync<JObject>();
      
      Log.Debug("vehicleDetails: {0}", vehicleDetails.Dump());

      vehicle.Details = vehicleDetails;

      var vehicleLocation = await _apiUrl
        .WithClient(_defaultHttpClient)
        .AppendPathSegments("v1", "accounts", userUid, "vehicles", vehicle.Vin, "location", "lastknown")
        .WithHeaders(WithAwsDefaultParameter(_apiKey))
        .AwsSign(awsCredentials, _awsEndpoint)
        .GetJsonAsync<VehicleLocation>();

      vehicle.Location = vehicleLocation;

      Log.Debug("vehicleLocation: {0}", vehicleLocation.Dump());
    }

    return vehicleResponse.Vehicles;
  }
}

