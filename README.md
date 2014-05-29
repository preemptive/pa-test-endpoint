# pa-test-endpoint

This is a simple endpoint for testing PreEmptive Analytics message transmission. The messages are captured and saved to disk in a `received` subdirectory of the current working directory. A separate file is created for each envelope received, named based on the envelope ID. 

Microsoft .NET 4.0 or greater is required for this application.

To compile, open the pa-test-endpoint.sln file in Visual Studio and build the solution under the Release configuration.

To run, run `endpoint.exe` from a command prompt, which will listen on port `8080` by default. 

## Argument Reference

`/h`               Prints this message.
`/p:portnum`       Specifies the port number to use (default 8080).
`/l:listeners`     Specifies the number of connection listeners (default 4 per CPU)
`/f`               Causes the endpoint to always return the 500 network response code.

## License   
The pa-test-endpoint is licensed under MS-PL, see License.txt for details. 
