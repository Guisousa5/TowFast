import { bootstrapApplication } from '@angular/platform-browser';
import { RuntimeConfigService } from './app/core/runtime-config.service';

RuntimeConfigService.load()
  .then(async () => {
    const [{ appConfig }, { App }] = await Promise.all([import('./app/app.config'), import('./app/app')]);
    return bootstrapApplication(App, appConfig);
  })
  .catch((err) => console.error(err));
