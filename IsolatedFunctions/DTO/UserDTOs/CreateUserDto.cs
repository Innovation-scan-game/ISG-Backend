using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace IsolatedFunctions.DTO.UserDTOs;

[OpenApiExample(typeof(CreateUserDtoExample))]
public class CreateUserDto
{
    [JsonRequired] public string Username { get; set; } = "";
    [JsonRequired] public string Password { get; set; } = "";
    [JsonRequired] public string Email { get; set; } = "";
}

public class CreateUserDtoExample : OpenApiExample<CreateUserDto>
{
    public override IOpenApiExample<CreateUserDto> Build(NamingStrategy? namingStrategy = null)
    {
        Examples.Add(OpenApiExampleResolver.Resolve("Joe",
            new CreateUserDto
            {
                Username = "Joe",
                Password = "secretPassw0rd",
                Email = "joe@mail.com"
            },
            namingStrategy));

        return this;
    }
}
