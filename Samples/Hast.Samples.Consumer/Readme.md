# Hastlayer consumer sample

A simple console application that showcases how an app can utilize Hastlayer and details everything in code comments. First head to the *Program.cs* file.

This is a complete, thoroughly documented sample. If you'd like to see a more stripped-down version of a minimal Hastlayer-using application, check out the *Hast.Samples.Demo* project instead.

## Using from the command line

We encourage you to check out the code of the app to understand how it works. You can also do all the configuration in code. However, if you'd just like to run it to test whether everything is OK in your setup then you can execute it as a command-line app, e.g.:

```
Hast.Samples.Consumer.exe -device "Alveo U50" -appname "MyApp" -appsecret "app secret" -sample "Loopback" -verify true
```

The following switches are available:

- `device`: The hardware device, i.e. FPGA board, to generate the hardware for and to communicate with. Check out the documentation for available ones.
- `appname` and `appsecret`: The name of your Hastlayer app that you created on hastlayer.com, and the corresponding secret (basically a password).
- `sample`: Which one of the sample algorithms to run. You can check out the available samples in the code, the concrete strings you can use here correspond to the values of the `Hast.Samples.Consumer.Sample` enum.
- `verify`: If It's `false` by default.
