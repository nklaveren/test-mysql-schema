# Relatório de Compatibilidade: Entity Framework Core 9 + MySQL com Schemas

## Contexto e Objetivo

Este documento detalha a implementação e validação do conceito de **schemas** no MySQL utilizando Entity Framework Core 9 com o provedor Pomelo. O objetivo é demonstrar como adaptar aplicações .NET que dependem de schemas lógicos para funcionarem corretamente com MySQL, que não possui suporte nativo a este recurso.

## O Problema: Schemas no MySQL

### Diferença entre SQL Server/PostgreSQL e MySQL

Em bancos de dados como **SQL Server** e **PostgreSQL**, o conceito de **schema** é uma estrutura organizacional que permite:

- Agrupar objetos de banco de dados (tabelas, views, procedures) em namespaces lógicos
- Isolar contextos de diferentes aplicações ou módulos no mesmo banco
- Facilitar a gestão de permissões e segurança
- Permitir estruturas como `schema.tabela` (ex: `sales.customers`, `hr.employees`)

**MySQL**, por outro lado, **não possui schemas como estrutura separada**. No MySQL:

- Um **database** é equivalente ao conceito de schema em outros SGBDs
- Não é possível ter múltiplos schemas dentro de um mesmo database
- A notação `database.tabela` é a forma de qualificar totalmente uma tabela

### Desafio em Arquiteturas Multi-squad

Em cenários de **microserviços** ou **multi-squad**, é comum que diferentes equipes ou domínios compartilhem a mesma infraestrutura de banco de dados, mas precisem de **isolamento lógico**:

- **Squad A** gerencia `vendas.clientes`, `vendas.pedidos`
- **Squad B** gerencia `estoque.produtos`, `estoque.movimentos`
- **Squad C** gerencia `financeiro.contas`, `financeiro.pagamentos`

Criar um database separado para cada squad pode ser excessivo e dificultar operações que precisam cruzar dados. A solução é simular schemas através de **prefixos nas tabelas**.

## Solução Implementada

### 1. Configuração do Provedor Pomelo

No arquivo `Program.cs`, o provedor MySQL é configurado com o comportamento de tradução de schemas:

```csharp
builder.Services.AddDbContext<SchemaTestDbContext>((serviceProvider, options) =>
{
    var connectionString = configuration.GetConnectionString("schemadb");
    var serverVersion = MySqlServerVersion.LatestSupportedServerVersion;

    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure();
        // Traduz schema lógico para prefixo físico na tabela
        mySqlOptions.SchemaBehavior(
            MySqlSchemaBehavior.Translate,
            (schema, entity) => $"{schema}_{entity}"
        );
    });
});
```

**O que acontece aqui:**

- `MySqlSchemaBehavior.Translate`: Instrui o provedor a traduzir schemas lógicos em nomes de tabelas físicas
- A função `(schema, entity) => $"{schema}_{entity}"` define a estratégia de tradução
- Resultado: `schematest.customers` → `schematest_customers`

### 2. Modelo de Dados com Schema Lógico

No `SchemaTestDbContext.cs`, o modelo define o schema de forma padrão do EF Core:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Define o schema padrão para todas as entidades
    modelBuilder.HasDefaultSchema("schematest");

    modelBuilder.Entity<Customer>(entity =>
    {
        // Mapeia para a tabela "customers" no schema "schematest"
        entity.ToTable("customers");

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
        entity.Property(e => e.Email).IsRequired().HasMaxLength(160);
        // ... outras configurações
    });

    // Aplicação de convenção: garante o prefixo em todas as tabelas
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        var tableName = entityType.GetTableName();

        if (!string.IsNullOrEmpty(tableName) &&
            !tableName.StartsWith("schematest_", StringComparison.OrdinalIgnoreCase))
        {
            entityType.SetTableName($"schematest_{tableName}");
        }
    }
}
```

**Vantagens desta abordagem:**

- O código da aplicação permanece **agnóstico ao banco de dados**
- Usa a API padrão do EF Core (`HasDefaultSchema`, `ToTable`)
- Fácil migração futura para PostgreSQL ou SQL Server
- Convenção automática aplicada a todas as entidades

## Resultado Observado

### Queries Geradas

O Entity Framework Core gera queries MySQL com os nomes de tabelas prefixados:

```sql
-- Verificação de existência de registros
SELECT EXISTS (
    SELECT 1
    FROM `schematest_customers` AS `c`
)

-- Listagem de clientes
SELECT `c`.`Id`, `c`.`Name`, `c`.`Email`, `c`.`CreatedAt`
FROM `schematest_customers` AS `c`
ORDER BY `c`.`Id`
```

### Estrutura Física no MySQL

No servidor MySQL, a tabela é criada com o nome físico combinado:

```sql
SHOW TABLES;
-- Resultado: schematest_customers

DESCRIBE schematest_customers;
-- +------------+-------------+------+-----+-------------------+-------------------+
-- | Field      | Type        | Null | Key | Default           | Extra             |
-- +------------+-------------+------+-----+-------------------+-------------------+
-- | Id         | int         | NO   | PRI | NULL              | auto_increment    |
-- | Name       | varchar(120)| NO   |     | NULL              |                   |
-- | Email      | varchar(160)| NO   |     | NULL              |                   |
-- | CreatedAt  | datetime(6) | NO   |     | CURRENT_TIMESTAMP | DEFAULT_GENERATED |
-- +------------+-------------+------+-----+-------------------+-------------------+
```

### Mapeamento Lógico vs Físico

| Camada               | Representação          |
| -------------------- | ---------------------- |
| **Código C# / EF**   | `schematest.customers` |
| **Query SQL Gerada** | `schematest_customers` |
| **Tabela MySQL**     | `schematest_customers` |

## Benefícios da Solução

### 1. Isolamento de Contextos

Múltiplas aplicações ou squads podem usar o mesmo database MySQL:

- `squad_vendas_clientes`, `squad_vendas_pedidos`
- `squad_estoque_produtos`, `squad_estoque_movimentos`
- `squad_financeiro_contas`, `squad_financeiro_pagamentos`

### 2. Portabilidade de Código

O mesmo código EF Core funciona em diferentes bancos:

```csharp
// Mesmo código funciona em SQL Server, PostgreSQL e MySQL
modelBuilder.HasDefaultSchema("schematest");
entity.ToTable("customers");
```

### 3. Organização e Clareza

Os prefixos facilitam:

- Identificação visual do contexto de cada tabela
- Busca e filtro no gerenciamento do banco
- Scripts de backup/restore seletivos
- Auditoria e rastreamento de mudanças

### 4. Compatibilidade com Ferramentas

Migrations do EF Core funcionam normalmente:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Casos de Uso Recomendados

### ✅ Quando Usar Esta Abordagem

1. **Microserviços com banco compartilhado**: Múltiplos serviços precisam de isolamento lógico
2. **Migração de SQL Server/PostgreSQL para MySQL**: Manter compatibilidade do código
3. **Ambientes multi-tenant**: Separar dados de diferentes clientes com prefixos
4. **Desenvolvimento modular**: Equipes diferentes trabalhando no mesmo database

### ⚠️ Quando Considerar Alternativas

1. **Isolamento completo necessário**: Use databases separados no MySQL
2. **Performance crítica**: Prefixos podem dificultar otimizações específicas
3. **Queries complexas entre schemas**: Joins podem ficar mais verbosos
4. **Restrições de nomenclatura**: Nomes de tabelas muito longos após prefixação

## Conclusões e Recomendações

### Principais Aprendizados

1. **MySQL não suporta schemas nativamente**, mas prefixos de tabela são uma solução eficaz e amplamente utilizada na indústria
2. O **provedor Pomelo para EF Core** oferece mecanismos flexíveis (`SchemaBehavior`) para traduzir schemas lógicos em convenções MySQL
3. A abordagem mantém o **código da aplicação portável** e compatível com as APIs padrão do Entity Framework
4. É possível **simular isolamento de contextos** em MySQL de forma elegante e mantível

### Boas Práticas

- **Defina uma convenção de nomenclatura clara**: Use prefixos curtos e descritivos
- **Documente a estratégia**: Garanta que toda a equipe entenda o mapeamento schema → tabela
- **Use migrations**: Aproveite o EF Core Migrations para versionamento do schema
- **Considere índices**: Planeje índices considerando os nomes completos das tabelas
- **Monitore o tamanho dos nomes**: MySQL tem limite de 64 caracteres para nomes de objetos

### Referências Técnicas

- [Pomelo.EntityFrameworkCore.MySql - Schema Translation](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- [MySQL Documentation - Database and Schema](https://dev.mysql.com/doc/)
- [Entity Framework Core - Model Configuration](https://learn.microsoft.com/ef/core/modeling/)

---

**Projeto**: SchemaTest POC  
**Stack**: .NET 9 + Entity Framework Core 9 + MySQL 8+ + Aspire  
**Data**: Outubro 2025
