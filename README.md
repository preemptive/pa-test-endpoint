# pa-test-endpoint

This is a simple endpoint for testing [PreEmptive Analytics](http://www.preemptive.com/pa) message transmission from an instrumented application. The messages received are stored on-disk in their raw formats.

Microsoft .NET 4.5 or greater is required for this application.

## Usage

1. Build the `pa-test-endpoint.sln` solution in Visual Studio and under the Release configuration.
2. Start the test endpoint by running `Test Endpoint\bin\Release\endpoint.exe`. Note the port indicated by the endpoint's startup.
3. Instrument the application to be tested, setting the endpoint to the machine and port the test endpoint is running on.
4. Test the instrumented application.
	* The messages received by the test endpoint are captured and saved to disk in a `received` subdirectory of the current working directory. A separate file is created for each envelope received, named based on the envelope ID. 
5. To stop the test endpoint, input `Ctrl+C`.

## Argument Reference

- `/h` - Prints this argument reference.
- `/p:portnum` - Port number to listen on (default `8080`).
- `/l:listeners` - Number of connection listeners (default 4 per CPU).
- `/f` - Always return the 500 network response code.
- `/slow:secs` - Wait &lt;secs&gt; seconds before each response to sender.
- `/nowrite` - Don't save incoming envelopes (or check for duplicates).
- `/perf` - Various changes to allow high throughput.

## License   
The pa-test-endpoint is licensed under MS-PL; see `License.txt` for details.
