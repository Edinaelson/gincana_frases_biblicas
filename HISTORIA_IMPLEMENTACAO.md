# História da Implementação: IA Local e RAG na Gincana Bíblica

Este documento registra a evolução técnica, os desafios enfrentados e as soluções aplicadas durante a implementação do modelo de linguagem **Llama** e do sistema de **RAG (Retrieval-Augmented Generation)** no projeto Gincana Passagens Bíblicas.

## 1. O Início: Migração para IA Local
O projeto originalmente dependia da API do Google Gemini. Para garantir independência, privacidade e custo zero, decidimos implementar o **Llama 3.1 e 3.2** rodando localmente via **Ollama**.

### Desafios Iniciais:
- **Integração**: Criamos a interface `IGeminiService` e o `OllamaService` para permitir a troca rápida entre nuvem e local apenas comentando uma linha no `Program.cs`.
- **Hardware**: Otimizamos o processamento para uma **GPU AMD RX 550 4GB** e um processador **i7-3770**.

## 2. A Crise dos Crashes (Erro -1)
Durante os testes, o sistema sofria quedas catastróficas (código de erro `-1 / 0xffffffff`) ao interagir com o seletor de imagens.

### O que tentamos e o que funcionou:
- **Falha**: O componente `InputFile` do Blazor Server no **.NET 10 Preview** era instável ao ter o diálogo de arquivos cancelado.
- **Solução Definitiva**: Removemos o componente nativo e implementamos um **input HTML puro com JavaScript Interop**. O arquivo passou a ser lido no navegador e enviado como Base64 para o C#, eliminando os crashes de memória e conexão.

## 3. O Desafio da Precisão Literal
IA generativas tendem a parafrasear textos. Em uma gincana bíblica, a precisão deve ser de 100% conforme a tradução **Almeida Revista e Corrigida (ARC)**.

### Evolução da Precisão:
1. **Llama Puro**: O modelo 3.1 8B era inteligente, mas trazia textos de versões modernas (NVI/ARA).
2. **RAG Simples**: Implementamos uma busca no arquivo `Portugues-All-Bible-Corrigida-Fiel.txt`. O Llama encontrava a referência, e o C# buscava o texto.
3. **Problema de Corte**: O arquivo TXT quebrava versículos em várias linhas. Ajustamos a lógica para ler o versículo completo até encontrar o próximo número.
4. **Similaridade Atômica**: Criamos um sistema de **Cache em Memória**. Agora, o C# carrega os 31.102 versículos na RAM (~3MB) e faz uma busca por palavras-chave antes de chamar a IA. O Llama agora atua como um "Árbitro", escolhendo a melhor opção entre resultados reais e literais do arquivo.

## 4. Otimização de Hardware e Load Balance
Para extrair o máximo de performance, utilizamos dois computadores na mesma rede:
- **Nó Local (i7-3770 + RX 550)**: Configurado para usar a GPU com `low_vram` e 35 camadas do modelo.
- **Nó Remoto (i5 11ª Geração)**: Adicionado via rede local (`192.168.18.251`).

### O Erro 500 no i5:
- **Descoberta**: O i5 tinha apenas 8GB de RAM, com pouco espaço livre. O modelo de 8B (4.8GB) causava erro de "Out of Memory".
- **Solução**: Implementamos modelos diferenciados por hardware. O i5 passou a usar uma versão **Quantizada (Light - q2_K)** ou o **Llama 3.2 3B**, enquanto o i7 manteve o modelo Full via GPU.

## 5. Performance e Benchmarks Reais
Durante os testes de estresse com o modelo **8B**, registramos variações de tempo significativas baseadas no estado do hardware:
- **Tempo Médio**: ~45 segundos (quando o modelo já está "quente" na VRAM/RAM).
- **Tempo de Pico**: ~1 minuto e 50 segundos (geralmente na primeira execução ou quando há disputa de recursos no Windows).
- **Fatores de Influência**: O uso de memória compartilhada devido ao limite de 4GB da RX 550 e a carga da rede local para o nó i5 são os principais motivos dessa oscilação.

## 6. Arquitetura Final Alcançada
- **Interface Visual**: Blazor Server com Radzen, usando identificadores visuais (Verde para correto, Vermelho para incorreto).
- **Processamento**: Híbrido entre CPU (8 threads) e GPU (RX 550).
- **Inteligência**: RAG Atômico que garante que o texto exibido venha diretamente do arquivo TXT oficial da Gincana.
- **Resiliência**: Lógica de Prioridade com Fallback (Tenta o notebook i5 primeiro; se falhar, usa o PC local).

---
**Data do Registro Final:** 19 de Fevereiro de 2026 (Simulação de Contexto).
**Status**: Estável, Preciso e Veloz.
