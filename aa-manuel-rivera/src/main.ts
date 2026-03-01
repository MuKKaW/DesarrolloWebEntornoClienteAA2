import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { defineRule } from 'vee-validate'
import { required, email, min, min_value, confirmed } from '@vee-validate/rules'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import App from './App.vue'
import router from './router'
import i18n from './i18n'
import './style.css'

defineRule('required', required)
defineRule('email', email)
defineRule('min', min)
defineRule('min_value', min_value)
defineRule('confirmed', confirmed)

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(i18n)
app.use(ElementPlus)

app.mount('#app')