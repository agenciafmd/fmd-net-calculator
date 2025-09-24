# F&MD .NET Calculator ➕✖️➖➗

Uma implementação gratuita de uma engine para calcular expressões matemáticas para nossos projetos .NET

| Pacote 📦|
| ------- |
| `Fmd.Net.Calculator` |

## Como usar?? 🤔

### Instalação ⬇️

Pode-se instalar o Calculator da F&MD via NuGet Package Manager ou através de linha de comando do .NET CLI

```bash
dotnet add package Fmd.Net.Calculator
```

## Uso com dicionário de variáveis

Esse exemplo demonstra como utilizar a engine com um dicionário de chave `string` e valor `decimal`

---

```csharp
var variables = new Dictionary<string, decimal>();
variables.Add("var1", 2.5);
variables.Add("var2", 3.4);

CalculationEngine engine = new CalculationEngine();
decimal result = engine.Calculate("(var1*var2)+4", variables);
```

Quando o método `Calculate` da engine é chamado, ele troca os itens da expressão que são chaves no dicionário por seu valor correspondente e devolve um resultado com alta precisão de casas decimais

---
## Atenção ⚠️

Os itens das chaves não podem conter pontos (.) pois este é um token aritimético e gera conflitos na execução do `Calculate()`

---

## Compatibilidade 🖇️

- Fmd.Net.Calculator utiliza o .NET 9 e exige no mínimo no projeto para sua utilização 🔥
- uma versão alta a fim de estimular a excelente prática da constante atualização de nossos projetos para as versões mais recentes do framework 🌟

---

## Sobre 💬

Fmd.Net.Calculator foi publicado por [Vinícius Fumagalli](www.linkedin.com/in/vini-fumagalli) sob a licença do MIT.