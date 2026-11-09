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
- As migrações SQLite encontram-se em `Data/Migrations/`. O arranque aplica-as automaticamente apenas em Development; em produção, gere e aplique um script revisto durante a publicação.
- `appsettings.json` aponta para o ficheiro SQLite local `gestorrhpro.db`, adequado ao desenvolvimento. Em produção, use um caminho num volume persistente e mantenha uma única instância; várias instâncias exigem mudar para um fornecedor de base de dados partilhado.
- As funções são criadas no arranque. Configure a conta inicial com User Secrets em desenvolvimento e com o gestor de segredos do alojamento em produção.
- O Identity exige palavra-passe com pelo menos 12 caracteres, maiúscula, minúscula, número e símbolo. O bloqueio após tentativas falhadas está ativo.
- Todas as páginas exigem autenticação por padrão. As áreas de gestão estão limitadas às funções `Administrator` e `HRManager`. Cada funcionário pode receber um acesso individual ao painel, criado pelo Administrador na lista de funcionários; a palavra-passe inicial é gerada uma vez e apresentada nessa resposta. O painel permite consultar assiduidade, férias e salários, e pedir férias.
- Administradores, RH e funcionários podem carregar uma fotografia JPG, PNG ou WebP de até 5 MB na página **O meu perfil**. As imagens ficam em `App_Data/ProfilePhotos`, fora da pasta pública, e cada utilizador acede à sua própria fotografia após iniciar sessão.
- Novos documentos são armazenados fora de `wwwroot` e transferidos por ações que verificam a propriedade. Documentos antigos em `/uploads` mantêm compatibilidade de leitura.
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

O projeto usa SQLite. A ligação padrão é `Data Source=gestorrhpro.db`; User Secrets são usados apenas em desenvolvimento para as credenciais do administrador inicial.

Em Development, o arranque aplica automaticamente as migrações. Fora desse ambiente, gere um script e aplique-o antes de iniciar a nova versão.

Para criar e aplicar futuras alterações ao modelo:

```powershell
dotnet ef migrations add NomeDaAlteracao
dotnet ef database update
```

Reveja cada migração antes de a aplicar. A antiga migração SQL Server foi substituída por `InitialSqlite`, pois migrações de um fornecedor não são portáveis para outro.

## Preparação para produção

A aplicação exige configuração explícita fora de Development. Defina estes valores no gestor de configuração/segredos do alojamento, não em ficheiros commitados:

- `AllowedHosts`: domínio real do site, sem `*`.
- `ConnectionStrings__DefaultConnection`: caminho absoluto para o ficheiro SQLite num volume persistente. O valor padrão é apenas para desenvolvimento local; para várias instâncias, migre para um fornecedor de base de dados partilhado.
- `DataProtection__KeysPath`: caminho absoluto para um diretório persistente e privado, partilhado por todas as instâncias da mesma aplicação. Proteja as chaves em repouso.
- `Storage__EmployeeDocumentsPath` e `Storage__ProfilePhotosPath`: caminhos absolutos para diretórios persistentes, privados e fora de `wwwroot`.
- `BootstrapAdmin__Email` e `BootstrapAdmin__Password`: credenciais iniciais guardadas como segredos do alojamento. Use uma palavra-passe nova para produção.

Por padrão, as migrações não são executadas ao iniciar em produção. Gere, reveja e aplique o SQL durante a publicação:

```powershell
dotnet ef migrations script --idempotent -o deploy.sql
```

Publique a aplicação em modo Release com `dotnet publish -c Release`. Configure HTTPS e cabeçalhos encaminhados conforme o proxy/plataforma, cópias de segurança automáticas e um procedimento de recuperação. Não publique ficheiros locais em `wwwroot/uploads`, bases de dados SQLite, documentos de funcionários nem chaves de Data Protection. O endpoint `/health` verifica a ligação à base de dados.

## Próximas etapas de desenvolvimento

Avançar uma etapa de cada vez: (1) estabilizar configuração e arranque seguro; (2) autenticação, autorização e configuração inicial de funções sem credenciais fixas; (3) validar o modelo e as migrações; (4) concluir departamentos e cargos; (5) funcionários; (6) assiduidade; (7) férias; (8) documentos; (9) relatórios e notificações. Os módulos de salários e tarefas agora permitem criar e gerir registos. Cada etapa deve ser verificada antes de iniciar a seguinte. Os módulos ainda não concluídos não devem ser apresentados como prontos para utilização empresarial.
