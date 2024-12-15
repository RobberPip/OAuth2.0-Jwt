using Utils;

var pass = Cryptography.HashPassword("test");

Console.WriteLine(Cryptography.ApplySalt(pass,"test"));