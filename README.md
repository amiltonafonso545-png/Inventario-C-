# GestorRH Pro

Aplicação de gestão de recursos humanos em C#, ASP.NET Core MVC, .NET 10, Entity Framework Core e SQLite.

O projeto está em desenvolvimento. O repositório já contém a aplicação em `GestorRHPro/`, com Identity, SQLite, dashboard, gestão inicial de funcionários, departamentos, assiduidade, férias, documentos, salários e tarefas. Nem todos os módulos estão concluídos ou prontos para produção.

## Arquitetura

```text
Navegador
   ↓
   Controllers/ → Services/ → Data/ApplicationDbContext → SQLite
   ↓                ↑
Views/ + ViewModels/ ┘

Areas/Identity: autenticação e gestão de contas
Models/: entidades do domínio
DTOs/: modelos de entrada e saída
wwwroot/: CSS, JavaScript e ficheiros estáticos
```

Os controladores coordenam pedidos MVC, os serviços concentram regras de negócio, o EF Core acede à base de dados e as vistas Razor apresentam ViewModels.

## Começar

1. Instale o [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0), o [Visual Studio Code](https://code.visualstudio.com/) e a extensão **C# Dev Kit**. O SQLite usado pelo projeto não precisa de um servidor separado.
2. Abra `GestorRHPro/` no VS Code.
3. No terminal integrado, execute:

   ```powershell
   dotnet restore
   dotnet build
   dotnet run
   ```

4. Abra o endereço indicado no terminal. O perfil HTTPS configurado usa `https://localhost:7064`.

Consulte [GestorRHPro/README.md](GestorRHPro/README.md) para os passos completos, comandos EF Core, estrutura de pastas e estado atual dos módulos.

## Conta inicial e acesso

Antes da primeira execução, configura o e-mail e a palavra-passe do administrador com User Secrets, conforme os passos em [GestorRHPro/README.md](GestorRHPro/README.md). Não existe uma palavra-passe padrão no código. O acesso às páginas exige início de sessão; a gestão está limitada às funções Administrador e RH.
