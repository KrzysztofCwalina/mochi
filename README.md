# mochi

Mochi is a personal assistant demonstrating Azure AI technologies. You can speak to her and she should respond back. She's a cat by birth, but you can convince her to become something else. Though she prefers her name to always remain 'Mochi'.

## Setup
For now, the application requires the user to manually setup Azure resources and store the resource endpoints, secrets, and other settings in environment variables. Here is the set of the resources:

### Azure Open AI 
Resource: https://ms.portal.azure.com/#view/Microsoft_Azure_ProjectOxford/CognitiveServicesHub/~/OpenAI

Env Vars:
- MOCHI_AI_ENDPOINT
- MOCHI_AI_KEY
- MOCHI_AI_MODEL

### Speech Service
This is required for speech recognition and synthesis

Env Vars:
- MOCHI_SPEECH_ENDPOINT
- MOCHI_SPEECH_KEY

### CLU (Conversational Language Understanding)

https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/conversational-language-understanding/overview

Train CLU model for the following intents: ChangePersona, GetTime

e.g. "What time is it?" and "You are a cat" or "Pretend to be a professor"

Env Vars:
- MOCHI_CLU_ENDPOINT
- MOCHI_CLU_KEY
- MOCHI_CLU_PROJECT_NAME
- MOCHI_CLU_DEPLOYMENT_NAME
