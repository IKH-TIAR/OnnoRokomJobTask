import { Link, Route, Routes } from 'react-router'
import { Layout } from './components/Layout'
import { AdminPage } from './pages/AdminPage'
import { ClassesPage } from './pages/ClassesPage'
import { ApplicationPage, MyApplicationsPage } from './pages/PlaceholderPages'

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<ClassesPage />} />
        <Route path="applications/:holdId" element={<ApplicationPage />} />
        <Route path="my-applications" element={<MyApplicationsPage />} />
        <Route path="admin" element={<AdminPage />} />
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  )
}

function NotFound() {
  return (
    <div className="flex flex-col gap-2">
      <h2 className="text-xl font-bold">Page not found</h2>
      <Link to="/" className="text-indigo-600 hover:underline">
        Go to classes
      </Link>
    </div>
  )
}
