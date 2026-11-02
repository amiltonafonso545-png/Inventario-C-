# GestorRH Pro

Aplicação web de gestão de recursos humanos em ASP.NET Core MVC, C#, .NET 10, Entity Framework Core e SQLite. A base de dados local é o ficheiro `gestorrhpro.db`; não é necessário instalar ou iniciar SQL Server. Este repositório já contém o projeto; não é necessário criar outro projeto por cima dele.

## Arquitetura

O projeto segue uma arquitetura MVC organizada por responsabilidades:

| Área | Responsabilidade |
| --- | --- |
| `Controllers/` | Receber pedidos HTTP e coordenar as operações MVC. |
| `Models/` | Entidades persistidas e modelos simples da aplicação. |
| `Data/` | `ApplicationDbContext` e migrações do Entity Framework Core. |
| `Views/` | Páginas Razor da interface MVC. |
| `Areas/Identity/` | Páginas de autenticação Identity (a personalizar conforme necessário). |
| `Services/` | Lógica de negócio a extrair dos controladores à medida que os módulos forem implementados. |
| `Repositories/` | Reservado para acesso a dados específico, caso a evolução o justifique. |
| `DTOs/` e `ViewModels/` | Modelos de entrada/saída e modelos próprios das páginas. |
| `Configuration/` | Configuração tipada e opções da aplicação. |
| `wwwroot/` | Ficheiros estáticos, como CSS, JavaScript e bibliotecas locais. |

Fluxo principal: navegador → controlador MVC → serviço de negócio → `ApplicationDbContext`/Entity Framework Core → SQLite (`gestorrhpro.db`). As vistas Razor recebem ViewModels e não devem conter regras de negócio. ASP.NET Core Identity gere contas, palavras-passe e funções.

### Estado encontrado no repositório

- O projeto visa `net10.0` e referencia ASP.NET Core Identity, EF Core e o fornecedor SQLite.
- O `Program.cs` configura MVC, Razor Pages, Identity, autenticação e autorização.
- Existem entidades para funcionários, departamentos, cargos, assiduidade, férias, documentos, salários mensais e tarefas, além de registos de auditoria e notificações no contexto de dados.
- Existem vistas e controladores iniciais, mas a presença de ficheiros não significa que os respetivos módulos estejam completos ou seguros.
- As migrações SQLite encontram-se em `Data/Migrations/` e o arranque tenta aplicá-las automaticamente.
- `appsettings.json` aponta para o ficheiro local `gestorrhpro.db`. O Entity Framework Core cria a base de dados ao iniciar a aplicação e aplicar a migração.
- As funções são criadas no arranque. A conta de administrador inicial só é criada quando as credenciais são configuradas localmente com User Secrets; não existe uma palavra-passe padrão no código.
- O Identity exige palavra-passe com pelo menos 12 caracteres, maiúscula, minúscula, número e símbolo. O bloqueio após tentativas falhadas está ativo.
- Todas as páginas exigem autenticação por padrão. As áreas de gestão estão limitadas às funções `Administrator` e `HRManager`; as páginas de autoatendimento do funcionário ainda não estão implementadas.
- Os ficheiros em `/uploads` só são servidos a utilizadores nas funções `Administrator` e `HRManager`.
- O módulo **Salários** regista salário base, suplementos e deduções por funcionário e mês; o valor líquido é calculado. A base impede registos duplicados para o mesmo funcionário e período.
- O módulo **Tarefas** permite criar, atribuir, editar, concluir e eliminar tarefas, com prazo, prioridade e estado.
- O dashboard não gera exemplos fictícios: começa sem valores de salários e tarefas. Registe funcionários primeiro; depois use **Salários → Novo salário** e **Tarefas → Nova tarefa** para preencher os cartões com dados da empresa.
- O ficheiro `app.db` que já existia é legado; a aplicação usa agora `gestorrhpro.db`. Ambos os ficheiros locais são ignorados pelo Git.

## Ferramentas necessárias

1. Instale o [SDK do .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0), não apenas o runtime.
2. Instale o [Visual Studio Code](https://code.visualstudio.com/).
3. No VS Code, instale a extensão **C# Dev Kit** da Microsoft.
4. Instale a ferramenta de linha de comandos do EF Core:

   ```powershell
   dotnet tool install --global dotnet-ef --version 10.0.14
   ```

   Se já estiver instalada, atualize-a com `dotnet tool update --global dotnet-ef --version 10.0.14`.

5. Antes da primeira execução, configure uma conta administrativa local. Na pasta `GestorRHPro`, execute os comandos abaixo e substitua os valores pelos seus:

   ```powershell
   dotnet user-secrets set "BootstrapAdmin:Email" "gestor@exemplo.pt"
   $securePassword = Read-Host "Escolhe uma palavra-passe forte" -AsSecureString
   $plainPassword = [System.Net.NetworkCredential]::new("", $securePassword).Password
   dotnet user-secrets set "BootstrapAdmin:Password" $plainPassword
   Remove-Variable securePassword, plainPassword
   ```

   A palavra-passe tem de ter pelo menos 12 caracteres, incluindo maiúsculas, minúsculas, número e símbolo. Não a partilhe nem a coloque em `appsettings.json`. Sem estas duas definições a aplicação não cria administrador. Se uma base de dados de uma versão anterior ainda contiver a conta fixa antiga, o arranque invalida a sua palavra-passe.

O .NET 10 é uma versão LTS, atualmente suportada pela Microsoft até novembro de 2028. Consulte a [política oficial de suporte .NET](https://dotnet.microsoft.com/platform/support/policy/dotnet-core) para confirmar datas e versões de manutenção atuais.

## Abrir e executar no VS Code

Abra a pasta `GestorRHPro` no VS Code, ou abra o repositório inteiro e use o terminal integrado:

```powershell
cd GestorRHPro
dotnet --version
dotnet restore
dotnet build
dotnet ef migrations list
dotnet run
```

O perfil de lançamento HTTPS usa `https://localhost:7064`; o perfil HTTP usa `http://localhost:5193`. Se necessário, confie o certificado de desenvolvimento HTTPS com `dotnet dev-certs https --trust`.

## Base de dados SQLite

O projeto usa SQLite e não requer servidor ou configuração de ligação. A ligação padrão em `appsettings.json` é `Data Source=gestorrhpro.db`. User Secrets são usados apenas para credenciais do administrador inicial.

O arranque aplica automaticamente a migração SQLite e cria o ficheiro da base de dados na pasta do projeto.

Para criar e aplicar futuras alterações ao modelo:

```powershell
dotnet ef migrations add NomeDaAlteracao
dotnet ef database update
```

Reveja cada migração antes de a aplicar. A antiga migração SQL Server foi substituída por `InitialSqlite`, pois migrações de um fornecedor não são portáveis para outro.

## Próximas etapas de desenvolvimento

Avançar uma etapa de cada vez: (1) estabilizar configuração e arranque seguro; (2) autenticação, autorização e configuração inicial de funções sem credenciais fixas; (3) validar o modelo e as migrações; (4) concluir departamentos e cargos; (5) funcionários; (6) assiduidade; (7) férias; (8) documentos; (9) relatórios e notificações. Os módulos de salários e tarefas agora permitem criar e gerir registos. Cada etapa deve ser verificada antes de iniciar a seguinte. Os módulos ainda não concluídos não devem ser apresentados como prontos para utilização empresarial.
