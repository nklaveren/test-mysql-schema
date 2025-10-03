# Relatório de compatibilidade EF Core + MySQL

## Contexto

Para validar como o Entity Framework Core 9 (via provedor Pomelo) trata esquemas quando executado sobre MySQL, foi adotada uma configuração que traduz o conceito de _schema_ do EF para um prefixo aplicado ao nome da tabela física.

## Configuração aplicada

```csharp
options.UseMySql(connectionString, serverVersion, mySqlOptions =>
{
    mySqlOptions.EnableRetryOnFailure();
    mySqlOptions.SchemaBehavior(MySqlSchemaBehavior.Translate, (schema, entity) => $"{schema}_{entity}");
});

entity.ToTable("customers", "schematest");
```

### Detalhes

- `SchemaBehavior(MySqlSchemaBehavior.Translate, ...)` instrui o provedor a traduzir o _schema_ informado na entidade para outro nome válido no MySQL.
- A função `(schema, entity) => $"{schema}_{entity}"` concatena o nome do schema lógico (`schematest`) com o nome da entidade (`customers`), produzindo o prefixo desejado.
- `entity.ToTable("customers", "schematest")` continua descrevendo o _schema_ lógico dentro do _ModelBuilder_ do EF Core, mantendo compatibilidade com o restante da aplicação.

## Resultado observado

A tabela física criada e consultada pelo EF Core passa a utilizar o prefixo informado:

```sql
SELECT EXISTS (
     SELECT 1
     FROM `schematest_customers` AS `c`
)
```

Ou seja, o EF Core trabalha com o modelo lógico `schematest.customers`, enquanto o MySQL hospeda a tabela `schematest_customers`. Esse mapeamento atende ao objetivo de compatibilidade sem exigir mudanças adicionais na camada de domínio.

## Conclusões

- É possível utilizar _schemas_ lógicos no modelo EF Core mesmo em bancos que não suportam o conceito nativamente (MySQL), desde que se traduza o nome na camada do provedor.
- A abordagem de prefixar o nome físico resolve conflitos e permite múltiplos _schemas_ lógicos compartilharem o mesmo servidor MySQL.
- Essa configuração serve como referência de compatibilidade mínima entre EF Core e MySQL para aplicações que dependem do recurso de _schema_.
