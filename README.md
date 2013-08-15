# pa-test-endpoint

This is a simple endpoint for testing PreEmptive Analytics message transmission. 
The messages are captured and saved to disk in the same directory as the executable. A separate file is created for each envelope received with 
the file name matching the envelope ID. 

Microsoft .NET 4.0 or greater is required for this application.

To compile, open the pa-test-endpoint.sln file in Visual Studio and build the solution under the Release configuration.

To run, run endpoint.exe from a command prompt. Running endpoint.exe will listen on localhost:8080 by default. You can specify other hosts and 
ports to listen on. Running "endpoint /p:8081 /h:127.0.0.2" will cause the endpoint to listen on 127.0.0.2:8081.

# Argument Reference

/? help  
/p:portnumber specify a port number  
/h:host specify the host name  
/f causes the endpoint to always return a 500.  
  
   
The pa-test-endpoint is licensed under MS-PL, look at License.txt for details. 